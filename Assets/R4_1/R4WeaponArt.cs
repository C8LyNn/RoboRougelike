using RoboRougelike.R4;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    // Keeps the old physical pivot/collider and follows its aim. The new mesh
    // contributes no collider and its corrected muzzle is used by the same gun.
    [DisallowMultipleComponent]
    public sealed class R4WeaponArt : MonoBehaviour
    {
        Transform physicalPivot;
        R4ArtWeaponAxisAdapter adapter;
        public Transform Muzzle=>adapter!=null?adapter.Muzzle:null;

        public static R4WeaponArt TryAttach(Transform block,Transform pivot,R4WeaponKind weapon)
        {
            // Saw needs its own rotor integration; Automatic has no supplied model.
            if(block==null||pivot==null||weapon==R4WeaponKind.RotarySaw||weapon==R4WeaponKind.Automatic)return null;
            var catalog=Resources.Load<R4WeaponArtCatalog>("R4Art/WeaponCatalog");
            var prefab=catalog!=null?catalog.ModelFor(weapon):null;
            if(prefab==null)return null;
            var visual=Instantiate(prefab,block,false);
            var axes=block.gameObject.AddComponent<R4ArtWeaponAxisAdapter>();
            if(!axes.Bind(visual.transform,"R4W_"+weapon))
            {Destroy(visual);Destroy(axes);return null;}
            var view=block.gameObject.AddComponent<R4WeaponArt>();view.physicalPivot=pivot;view.adapter=axes;
            foreach(var renderer in pivot.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
            view.Sync();return view;
        }

        public void Sync()
        {
            if(physicalPivot==null||adapter==null)return;
            var direction=transform.InverseTransformDirection(physicalPivot.forward).normalized;
            float yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
            float pitch=Mathf.Atan2(direction.y,Mathf.Sqrt(direction.x*direction.x+direction.z*direction.z))*Mathf.Rad2Deg;
            adapter.SetAim(yaw,pitch);
        }
        void LateUpdate()=>Sync();
    }
}
