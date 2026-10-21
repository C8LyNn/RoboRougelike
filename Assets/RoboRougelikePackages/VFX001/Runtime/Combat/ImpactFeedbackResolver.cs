using UnityEngine;

namespace RoboRougelike.VFX001
{
    public static class ImpactFeedbackResolver
    {
        public static ImpactFeedbackSpec Resolve(ImpactMaterialType materialType, float impact01)
        {
            impact01 = Mathf.Clamp01(impact01);
            var spec = ImpactFeedbackSpec.Default;

            switch (materialType)
            {
                case ImpactMaterialType.Metal:
                    spec.particleCount = Mathf.RoundToInt(Mathf.Lerp(8f, 18f, impact01));
                    spec.speed = Mathf.Lerp(4f, 9f, impact01);
                    spec.lifeTime = Mathf.Lerp(0.25f, 0.45f, impact01);
                    spec.size = Mathf.Lerp(0.06f, 0.12f, impact01);
                    spec.color = new Color(1f, 0.82f, 0.35f);
                    spec.emissiveFlash = true;
                    break;

                case ImpactMaterialType.Rubber:
                    spec.particleCount = Mathf.RoundToInt(Mathf.Lerp(4f, 8f, impact01));
                    spec.speed = Mathf.Lerp(2.5f, 5f, impact01);
                    spec.lifeTime = Mathf.Lerp(0.18f, 0.28f, impact01);
                    spec.size = Mathf.Lerp(0.05f, 0.1f, impact01);
                    spec.color = new Color(0.25f, 0.25f, 0.25f);
                    spec.emissiveFlash = false;
                    break;

                case ImpactMaterialType.Energy:
                    spec.particleCount = Mathf.RoundToInt(Mathf.Lerp(8f, 14f, impact01));
                    spec.speed = Mathf.Lerp(3f, 7f, impact01);
                    spec.lifeTime = Mathf.Lerp(0.2f, 0.38f, impact01);
                    spec.size = Mathf.Lerp(0.07f, 0.11f, impact01);
                    spec.color = new Color(0.35f, 0.85f, 1f);
                    spec.emissiveFlash = true;
                    break;

                case ImpactMaterialType.Engine:
                    spec.particleCount = Mathf.RoundToInt(Mathf.Lerp(7f, 15f, impact01));
                    spec.speed = Mathf.Lerp(3.5f, 7.5f, impact01);
                    spec.lifeTime = Mathf.Lerp(0.24f, 0.42f, impact01);
                    spec.size = Mathf.Lerp(0.07f, 0.12f, impact01);
                    spec.color = Color.Lerp(new Color(1f, 0.55f, 0.1f), new Color(1f, 0.85f, 0.45f), 0.35f);
                    spec.emissiveFlash = true;
                    break;

                case ImpactMaterialType.Core:
                    spec.particleCount = Mathf.RoundToInt(Mathf.Lerp(10f, 20f, impact01));
                    spec.speed = Mathf.Lerp(4f, 8.5f, impact01);
                    spec.lifeTime = Mathf.Lerp(0.3f, 0.5f, impact01);
                    spec.size = Mathf.Lerp(0.08f, 0.14f, impact01);
                    spec.color = new Color(1f, 0.25f, 0.25f);
                    spec.emissiveFlash = true;
                    break;

                default:
                    spec.particleCount = Mathf.RoundToInt(Mathf.Lerp(5f, 10f, impact01));
                    spec.speed = Mathf.Lerp(3f, 6f, impact01);
                    spec.lifeTime = Mathf.Lerp(0.2f, 0.3f, impact01);
                    spec.size = Mathf.Lerp(0.05f, 0.09f, impact01);
                    spec.color = new Color(1f, 0.8f, 0.45f);
                    spec.emissiveFlash = true;
                    break;
            }

            return spec;
        }
    }
}
