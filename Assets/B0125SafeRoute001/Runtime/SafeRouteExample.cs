namespace B01.LevelChallenges.SafeRoute
{
    public static class SafeRouteExample
    {
        public static LevelGrid Create()
        {
            return LevelGrid.Parse(
                "###############",
                "#......#......#",
                "#.S....#....E.#",
                "#......G......#",
                "#......G..HH..#",
                "#......G......#",
                "#......#......#",
                "#.............#",
                "#.............#",
                "#.............#",
                "###############");
        }
    }
}
