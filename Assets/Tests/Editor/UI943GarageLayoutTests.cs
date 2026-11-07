using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R3;
using UnityEngine;

public sealed class UI943GarageLayoutTests
{
    [TestCase(1920,1080)] [TestCase(1366,768)] [TestCase(1024,768)]
    [TestCase(768,1024)] [TestCase(3840,2160)]
    public void CameraViewportMatchesLetterboxedGarageOpening(int width,int height)
    {
        var method=typeof(R3GarageView).GetMethod("ViewportForScreen",BindingFlags.Public|BindingFlags.Static);
        Assert.That(method,Is.Not.Null,"Kamera i HUD potrzebują wspólnej transformacji.");
        var actual=(Rect)method.Invoke(null,new object[]{width,height});
        if(height>width)
        {
            float portraitScale=Mathf.Min(width/768f,height/1024f);
            float x=(width-768*portraitScale)/2, y=(height-1024*portraitScale)/2;
            var opening=R3GarageView.PortraitOpening;
            Assert.That(actual.x*width,Is.EqualTo(x+opening.x*portraitScale).Within(.1f));
            Assert.That(actual.y*height,Is.EqualTo(height-y-opening.yMax*portraitScale).Within(.1f));
            Assert.That(actual.width*width,Is.EqualTo(opening.width*portraitScale).Within(.1f));
            Assert.That(actual.height*height,Is.EqualTo(opening.height*portraitScale).Within(.1f));
            return;
        }
        float scale=Mathf.Min(width/1280f,height/720f);
        float left=(width-1280*scale)/2, bottom=(height-720*scale)/2;
        var expected=R3GarageView.Viewport;
        Assert.That(actual.x*width,Is.EqualTo(left+expected.x*1280*scale).Within(.1f));
        Assert.That(actual.y*height,Is.EqualTo(bottom+expected.y*720*scale).Within(.1f));
        Assert.That(actual.width*width,Is.EqualTo(expected.width*1280*scale).Within(.1f));
        Assert.That(actual.height*height,Is.EqualTo(expected.height*720*scale).Within(.1f));
    }

    [TestCase(1920,1080)] [TestCase(1366,768)] [TestCase(1024,768)]
    [TestCase(768,1024)] [TestCase(3840,2160)]
    public void GarageOverlaysCannotReceivePlacementClicks(int width,int height)
    {
        if(height>width)
        {
            float portraitScale=Mathf.Min(width/768f,height/1024f);
            float x=(width-768*portraitScale)/2, y=(height-1024*portraitScale)/2;
            Vector2 PortraitPoint(float px,float py)=>new Vector2(x+px*portraitScale,height-(y+py*portraitScale));
            Assert.That(R3GarageView.InBuildArea(PortraitPoint(384,300),width,height),Is.True);
            foreach(var p in new[]{new Vector2(384,60),new Vector2(384,530),new Vector2(10,300)})
                Assert.That(R3GarageView.InBuildArea(PortraitPoint(p.x,p.y),width,height),Is.False);
            return;
        }
        float scale=Mathf.Min(width/1280f,height/720f);
        float left=(width-1280*scale)/2, top=(height-720*scale)/2;
        Vector2 ScreenPoint(float x,float y)=>new Vector2(left+x*scale,height-(top+y*scale));
        foreach(var p in new[]{new Vector2(500,100),new Vector2(500,140),new Vector2(500,180),new Vector2(500,430),new Vector2(500,480)})
            Assert.That(R3GarageView.InBuildArea(ScreenPoint(p.x,p.y),width,height),Is.False,$"Nakładka przy {p} nie może montować części.");
        Assert.That(R3GarageView.InBuildArea(ScreenPoint(640,300),width,height),Is.True,"Odsłonięty środek nadal służy do montażu.");
        Assert.That(R3GarageView.InBuildArea(ScreenPoint(934.8f,300),width,height),Is.False,"Nie klikaj poza obrazem kamery.");
    }
}
