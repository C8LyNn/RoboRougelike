using UnityEngine;

namespace RoboRougelike.R3
{
    public static class R3VehicleCatalog
    {
        public static R3VehicleDesign Create(R3VehicleKind kind)
        {
            bool light = kind == R3VehicleKind.Light, heavy = kind == R3VehicleKind.Heavy;
            var design = new R3VehicleDesign
            {
                Name = light ? "Lekki" : heavy ? "Ciężki" : "Standard",
                Description = light ? "Zwrotny zwiadowca: 4 koła, mały kadłub, odsłonięty rdzeń."
                    : heavy ? "Pancerny kolos: 12 kół, dwuwarstwowy szeroki kadłub, rdzeń 180 HP."
                    : "Uniwersalny pojazd: 12 kół, dwie warstwy pancerza, dobra baza do rozbudowy.",
                Speed = light ? 10 : heavy ? 5.5f : 7.5f,
                TurnSpeed = light ? 105 : heavy ? 60 : 82,
                CoreHp = light ? 80 : heavy ? 180 : 100
            };
            int width = light ? 1 : heavy ? 3 : 2;
            int length = light ? 2 : heavy ? 4 : 3;
            int layers = light ? 1 : 2;
            for (int y = 0; y < layers; y++)
                for (int x = -width; x <= width; x++)
                    for (int z = -length; z <= length; z++)
                        design.Parts.Add(new R3PartState(new Vector3Int(x, y, z),
                            x == 0 && y == 0 && z == 0 ? R3PartKind.Core : R3PartKind.Armor));
            int wheels = light ? 2 : 6;
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < wheels; i++)
                    design.Parts.Add(new R3PartState(new Vector3Int(side * (width + 1), 0,
                        light ? i * 2 - 1 : i - 3), R3PartKind.Wheel));
            design.Parts.Add(new R3PartState(new Vector3Int(0, layers, 0), R3PartKind.Gun));
            return design;
        }
    }
}
