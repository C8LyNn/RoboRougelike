import copy
import unittest
from qa_w16 import verify, InvalidEvidence

S='a'*40
H='b'*64

def chl():
    ids=['C1','C2','C3']; rigs=[dict(id=k,mass_kg=m,width_m=w,rigidbody=True) for k,m,w in [('Light',100,2),('Heavy',250,4)]]
    trials=[]
    for rig in rigs:
        for rep in range(1,4):
            events=[{'type':'begin','actor':rig['id']}]+[{'type':'checkpoint','actor':rig['id'],'id':c} for c in ids]+[{'type':'finish','actor':rig['id']}]
            trials.append(dict(rig_id=rig['id'],replicate=rep,seed=7,physics_engine='Unity.PhysX',fixed_steps=600,displacement_m=40.0,wheel_contact_samples=400,control_samples=500,telemetry_sha256=H,events=events))
    return dict(schema='b01-60-w16-qa-input-v1',package_id='CHL-003',version='R1',source_sha=S,origin='SYNTHETIC_TEST',native_status='NOT_RUN',course=dict(seed=7,checkpoint_ids=ids),rigs=rigs,trials=trials)

def partsize():
    samples=[dict(yaw_degrees=y,collider_aabb_m=([.5,.5,1.05] if y in (0,180) else [1.05,.5,.5]),physics_api='Unity.Physics',penetration_m=0.0) for y in (0,90,180,270)]
    part=dict(id='BonusModule',cells=[1,1,2],physical_owner_id='part-single-owner',mesh_sha256=H,samples=samples)
    return dict(schema='b01-60-w16-qa-input-v1',package_id='PARTSIZE-001',version='R1',source_sha=S,origin='SYNTHETIC_TEST',native_status='NOT_RUN',grid_m=.55,parts=[part])

class TestCHL(unittest.TestCase):
    def reject(self, mut, code):
        v=chl();mut(v)
        with self.assertRaisesRegex(InvalidEvidence,code): verify(v)
    def test_consistent_synthetic_is_not_native_pass(self):
        x=verify(chl());self.assertEqual(x['trials_checked'],6);self.assertEqual(x['independent_native_unity'],'NOT_RUN')
    def test_missing_checkpoint(self): self.reject(lambda v:v['trials'][0]['events'].pop(1),'EVENTS_INCOMPLETE')
    def test_duplicate_checkpoint(self): self.reject(lambda v:v['trials'][0]['events'].insert(2,dict(type='checkpoint',actor='Light',id='C1')),'BAD_CHECKPOINT')
    def test_wrong_actor(self): self.reject(lambda v:v['trials'][0]['events'][1].update(actor='Heavy'),'FOREIGN_ACTOR')
    def test_early_finish(self): self.reject(lambda v:v['trials'][0]['events'][1].update(type='finish'),'EARLY_FINISH')
    def test_two_finishes(self): self.reject(lambda v:v['trials'][0]['events'].append(dict(type='finish',actor='Light')),'EVENT_AFTER')
    def test_no_physics(self): self.reject(lambda v:v['trials'][0].update(physics_engine='Unity.Fake'),'NOT_REAL_PHYSICS')
    def test_missing_contact(self): self.reject(lambda v:v['trials'][0].update(wheel_contact_samples=0),'NO_CONTACT')
    def test_negative_velocity_nan(self): self.reject(lambda v:v['trials'][0].update(displacement_m=float('nan')),'MISSING_MOTION')
    def test_duplicate_trial(self): self.reject(lambda v:v['trials'][1].update(replicate=1),'DUPLICATE_TRIAL')
    def test_identical_rigs(self): self.reject(lambda v:v['rigs'][1].update(mass_kg=100,width_m=2),'RIGS_NOT')
    def test_native_claim_synthetic(self): self.reject(lambda v:v.update(native_status='EXTERNAL_UNVERIFIED'),'SYNTHETIC_CLAIMS')
    def test_telemetry_unhashed(self): self.reject(lambda v:v['trials'][0].update(telemetry_sha256='bogus'),'MISSING_TELEMETRY')
    def test_seed_mismatch(self): self.reject(lambda v:v['trials'][0].update(seed=31),'SEED_MISMATCH')

class TestPARTSIZE(unittest.TestCase):
    def reject(self, mut,code):
        v=partsize();mut(v)
        with self.assertRaisesRegex(InvalidEvidence,code):verify(v)
    def test_consistent_synthetic_still_not_native(self):
        r=verify(partsize());self.assertEqual(r['rotations_checked'],4);self.assertEqual(r['independent_native_unity'],'NOT_RUN')
    def test_missing_yaw_270(self): self.reject(lambda v:v['parts'][0]['samples'].pop(),'NEED_FOUR_YAWS')
    def test_duplicate_yaw(self): self.reject(lambda v:v['parts'][0]['samples'][1].update(yaw_degrees=0),'INVALID_YAW')
    def test_rotation_does_not_swap(self): self.reject(lambda v:v['parts'][0]['samples'][1].update(collider_aabb_m=[.5,.5,1.05]),'COLLIDER_EXCEEDS_LOGICAL_FOOTPRINT')
    def test_excess_envelope(self): self.reject(lambda v:v['parts'][0]['samples'][0].update(collider_aabb_m=[.60,.5,1.05]),'COLLIDER_EXCEEDS')
    def test_physics_overlap(self): self.reject(lambda v:v['parts'][0]['samples'][2].update(penetration_m=.31),'OVERLAPPING')
    def test_missing_owner(self): self.reject(lambda v:v['parts'][0].update(physical_owner_id=''),'OWNER_MISSING')
    def test_nan_dimensions(self): self.reject(lambda v:v['parts'][0]['samples'][0].update(collider_aabb_m=[float('nan'),.5,1.05]),'INVALID_COLLIDER')
    def test_grid_mismatch(self): self.reject(lambda v:v.update(grid_m=.52),'GRID_MISMATCH')
    def test_duplicate_part(self): self.reject(lambda v:v['parts'].append(copy.deepcopy(v['parts'][0])),'DUPLICATE_OR_MISSING')
    def test_tampered_mesh_digest(self): self.reject(lambda v:v['parts'][0].update(mesh_sha256='0'),'MESH_SOURCE_UNHASHED')
    def test_invalid_cells(self): self.reject(lambda v:v['parts'][0].update(cells=[1,0,2]),'INVALID_CELLS')
    def test_claim_real_unverified(self): self.reject(lambda v:v.update(origin='EXTERNAL_PRODUCER',native_status='NOT_RUN'),'EXTERNAL_ORIGIN_STATUS')

if __name__=='__main__':unittest.main()
