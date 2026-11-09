using UnityEngine;

namespace RoboRougelike.R3
{
    public static class R3VehicleCatalog
    {
        public static int StartingScrap(R3VehicleKind kind)=>kind==R3VehicleKind.Light?200:kind==R3VehicleKind.Standard?75:0;
        public static int[] DrawOffers(System.Random random)
        {
            if(random==null)throw new System.ArgumentNullException(nameof(random));
            var pool=new int[12];for(int i=0;i<pool.Length;i++)pool[i]=i;
            for(int i=0;i<3;i++)
            {int j=random.Next(i,pool.Length);int value=pool[i];pool[i]=pool[j];pool[j]=value;}
            return new[]{pool[0],pool[1],pool[2]};
        }
        public static R3VehicleDesign Create(R3VehicleKind kind)=>Create(kind,0);
        public static R3VehicleDesign Create(R3VehicleKind kind,int variant)
        {
            variant=Mathf.Clamp(variant,0,3);
            bool light=kind==R3VehicleKind.Light,heavy=kind==R3VehicleKind.Heavy;
            string[] names=light?new[]{"Zwiadowca","Skoczek","Strzelec","Łazik"}:
                heavy?new[]{"Bastion","Młot","Forteca","Lewiatan"}:
                new[]{"Patrol","Szturm","Eskorta","Wędrowiec"};
            var design=new R3VehicleDesign {
                Name=names[variant],
                Description=(light?"Lekki":heavy?"Ciężki":"Średni")+" • złom na start: "+StartingScrap(kind),
                // Shared baseline: actual mass and wheels determine mobility.
                Speed=7.5f,TurnSpeed=82,CoreHp=100
            };
            int width=light?2:3,length=heavy?4:3,upperLength=light?2:3;
            for(int x=-width;x<=width;x++)for(int z=-length;z<=length;z++)
                design.Parts.Add(new R3PartState(new Vector3Int(x,0,z),x==0&&z==0?R3PartKind.Core:R3PartKind.Armor));
            for(int x=-2;x<=2;x++)for(int z=-upperLength;z<=upperLength;z++)
            {
                bool gunSupport=z==0&&(light?x==0:!heavy&&Mathf.Abs(x)==2||heavy&&x==0);
                bool omit=!gunSupport&&(variant==1&&Mathf.Abs(x)==2&&Mathf.Abs(z)==upperLength||
                    variant==2&&Mathf.Abs(x)==2&&Mathf.Abs(z)==1||
                    variant==3&&Mathf.Abs(x)==1&&Mathf.Abs(z)==upperLength);
                if(omit)continue;
                bool battery=z==0&&(heavy?Mathf.Abs(x)<=1:light?x==0:x==0||x==1);
                design.Parts.Add(new R3PartState(new Vector3Int(x,1,z),battery?R3PartKind.Battery:R3PartKind.Armor));
            }
            // Different front and rear profiles give each version a distinct
            // silhouette while every armor block still has support beneath it.
            for(int x=-2;x<=2;x++)
            {
                if(upperLength<length&&((variant==0&&Mathf.Abs(x)<=1)||(variant==2&&Mathf.Abs(x)==2)||(variant==3&&Mathf.Abs(x)==1)))
                    design.Parts.Add(new R3PartState(new Vector3Int(x,1,upperLength+1),R3PartKind.Armor));
                if(upperLength<length&&((variant==1&&Mathf.Abs(x)<=1)||(variant==3&&Mathf.Abs(x)==1)))
                    design.Parts.Add(new R3PartState(new Vector3Int(x,1,-upperLength-1),R3PartKind.Armor));
                foreach(int side in new[]{-1,1})
                {
                    var support=new Vector3Int(x,1,side*upperLength);
                    if(design.Parts.Exists(p=>p.Cell==support))
                        design.Parts.Add(new R3PartState(support+Vector3Int.up,R3PartKind.Armor));
                }
            }
            // Front armor intercepts head-on impacts before the low wheel centers,
            // reducing nose-up torque without suppressing physics.
            if(heavy)
                foreach(int x in new[]{-3,3})design.Parts.Add(new R3PartState(new Vector3Int(x,1,0),R3PartKind.Armor));
            if(light)
                foreach(int x in new[]{-3,3})design.Parts.Add(new R3PartState(new Vector3Int(x,1,0),R3PartKind.Armor));
            int[] positions=light?(variant%2==0?new[]{-2,2}:new[]{-2,0,2}):
                heavy?(variant==3?new[]{-3,-2,0,2,3}:new[]{-3,-1,1,3}):
                variant==2?new[]{-3,-1,1,3}:new[]{-2,0,2};
            for(int side=-1;side<=1;side+=2)foreach(int z in positions)
                design.Parts.Add(new R3PartState(new Vector3Int(side*(width+1),0,z),R3PartKind.Wheel));
            if(light)design.Parts.Add(new R3PartState(new Vector3Int(0,2,0),R3PartKind.Gun));
            else if(heavy)for(int x=-3;x<=3;x+=3)design.Parts.Add(new R3PartState(new Vector3Int(x,2,0),R3PartKind.Gun));
            else for(int x=-2;x<=2;x+=4)design.Parts.Add(new R3PartState(new Vector3Int(x,2,0),R3PartKind.Gun));
            if(variant==1||variant==3)
                foreach(var gun in design.Parts)
                    if(gun.Kind==R3PartKind.Gun&&(light||gun.Cell.x>0))gun.Weapon=RoboRougelike.R4.R4WeaponKind.Automatic;
            // Starter blueprints use the same physical attachment faces as newly
            // installed equipment. Existing saved designs keep their stored masks.
            foreach(var part in design.Parts)
                if(part.Kind==R3PartKind.Wheel||part.Kind==R3PartKind.Gun)
                {
                    part.ContactMask=part.Kind==R3PartKind.Wheel?
                        R3MountingGrid.FaceBit(part.Cell.x<0?Vector3Int.right:Vector3Int.left):
                        R3VehicleModel.PlacementContacts(part.Kind,part);
                    if(part.Kind==R3PartKind.Gun)part.FootprintRevision=R3VehicleModel.NewPurchaseFootprint(part.Kind,part.Weapon,part.WheelStyle);
                }
            return design;
        }
    }
}
