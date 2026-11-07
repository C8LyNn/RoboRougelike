using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using UnityEngine;

public sealed class UI943NoticeLayoutTests
{
    [TestCase(1920,1080)] [TestCase(1366,768)] [TestCase(1024,768)]
    [TestCase(768,1024)] [TestCase(3840,2160)]
    public void ThreeDamageNoticesDoNotCoverSuspensionOrRangeControls(int width,int height)
    {
        var method=typeof(R1RunHUD).GetMethod("DamageNoticePanelRect",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null,"Powiadomienia wymagają bezpiecznego obszaru.");
        float scale=Mathf.Min(width/1280f,height/720f);
        var offset=new Vector2((width-1280*scale)/2,(height-720*scale)/2);
        Rect ScreenRect(Rect r)=>new Rect(offset.x+r.x*scale,offset.y+r.y*scale,r.width*scale,r.height*scale);
        var suspension=ScreenRect(new Rect(925,355,335,160));
        var bottom=ScreenRect(new Rect(18,625,1244,78));
        var pause=ScreenRect(new Rect(1130,670,125,35));
        Rect previous=new Rect();
        for(int i=0;i<3;i++)
        {
            var notice=ScreenRect((Rect)method.Invoke(null,new object[]{i}));
            Assert.That(notice.Overlaps(suspension),Is.False,"Ostrzeżenie nie może zasłaniać informacji o zawieszeniu.");
            Assert.That(notice.Overlaps(bottom),Is.False,"Ostrzeżenie nie może znikać pod panelem poligonu.");
            Assert.That(notice.Overlaps(pause),Is.False);
            if(i>0)Assert.That(notice.Overlaps(previous),Is.False);
            previous=notice;
        }
    }
}
