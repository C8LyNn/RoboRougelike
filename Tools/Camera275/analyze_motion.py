"""Frame-normalized camera measurements, separate from human comfort acceptance."""
import csv,json,math,pathlib,statistics,sys

def rms(values):return math.sqrt(statistics.mean(v*v for v in values)) if values else 0
def quantile(values,p):
    values=sorted(values)
    return values[min(len(values)-1,int((len(values)-1)*p))] if values else 0
def angle_delta(a,b):return (a-b+180)%360-180
def angles(row,prefix):
    x,y,z,w=(row[prefix+c] for c in ['Qx','Qy','Qz','Qw'])
    return math.degrees(math.atan2(2*(x*z+w*y),1-2*(x*x+y*y))),math.degrees(math.asin(max(-1,min(1,-2*(y*z-w*x)))))
def corners_in_view(row):
    # Reconstruct conservative raw-hull corners from actual recorded final pose.
    # FOV60 is fixed in scene1; production never changes it. Both recorded sizes
    # are 16:9. This is post-analysis, not an in-game mesh-visibility assertion.
    x,y,z,w=(row['cam'+c] for c in ['Qx','Qy','Qz','Qw'])
    def cross(a,b):return(a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
    for i in range(8):
        v=tuple(row['bounds'+axis]+(-1 if (i&(1<<n))==0 else 1)*row['extent'+axis]-row['cam'+axis] for n,axis in enumerate('XYZ'))
        c=cross((x,y,z),v);cc=cross((x,y,z),c)
        local=tuple(v[n]-2*w*c[n]+2*cc[n] for n in range(3))
        if local[2]<=0 or abs(local[0])>local[2]*math.tan(math.pi/6)*16/9 or abs(local[1])>local[2]*math.tan(math.pi/6):return False
    return True
def analyze(folder):
    with (folder/'camera-drive.csv').open() as stream:rows=[{k:float(v) for k,v in row.items()} for row in csv.DictReader(stream) if None not in row]
    speeds=[];vertical_acc=[];body_acc=[];orbit_errors=[];pixel_total=0;yaw_total=0;fit_rates=[];dts=[];previous=None;old_cv=old_bv=None
    for row in rows:
        dt=row['dt'];dts.append(dt)
        if previous is not None and dt>0:
            cy,cp=angles(row,'cam');py,pp=angles(previous,'cam')
            if row['phase']==5:
                pixel_total+=row['mouseX'];yaw_total+=angle_delta(cy,py)
                if row['clipped']==0:orbit_errors.append(abs(angle_delta(cy,row['orbitYaw'])))
            if row['phase']==3 and row['speed']>.3 and previous['phase']==3 and row['mouseX']==row['mouseY']==row['clipped']==0:
                speeds.append(math.hypot(angle_delta(cy,py),cp-pp)/dt)
                cv=(row['camY']-previous['camY'])/dt;bv=(row['rootY']-previous['rootY'])/dt
                if old_cv is not None:vertical_acc.append((cv-old_cv)/dt);body_acc.append((bv-old_bv)/dt)
                old_cv,old_bv=cv,bv
                fit_rates.append(abs(row['fit']-previous['fit'])/dt)
            else:old_cv=old_bv=None
        previous=row
    result=dict(evidence=str(folder),frames=len(rows),elapsed=rows[-1]['time']-rows[0]['time'] if rows else 0,
        medianFPS=1/statistics.median(dts) if dts else 0,slowFrameMs=quantile(dts,.99)*1000,
        uncommandedAngularSpeedRMS=rms(speeds),uncommandedAngularSpeedP95=quantile(speeds,.95),uncommandedAngularSpeedMax=max(speeds,default=0),
        cameraVerticalAccelerationRMS=rms(vertical_acc),bodyVerticalAccelerationRMS=rms(body_acc),fitRateRMS=rms(fit_rates),
        freeOrbitYawErrorRMS=rms(orbit_errors),actualYawDegreesPerInputPixel=yaw_total/pixel_total if pixel_total else None,
        selfRayFrames=sum(r['selfHit']!=0 for r in rows),worldOverlapFrames=sum(r['overlap']!=0 for r in rows),
        conservativeCornerOffscreenFrames=sum(not corners_in_view(r) for r in rows if not r['clipped']),
        projectionQualification='Derived from final native pose + raw AABB, frozen scene FOV60 and 16:9; unclipped frames only. Conservative bounds, not direct mesh/occlusion measurement.',
        resetCountChange=max((r['resets'] for r in rows),default=0)-min((r['resets'] for r in rows),default=0),
        qualification='Actual native drive trace; not human comfort acceptance. Motion statistics exclude deliberate mouse input/collision and stationary frames as stated.')
    (folder/'motion-summary.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    return result
if __name__=='__main__':
    for arg in sys.argv[1:]:print(json.dumps(analyze(pathlib.Path(arg)),indent=2))
