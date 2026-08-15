internal static class CharacterCommonModuleTestRunner
{
    public static int Main()
    {
        int failures = CharacterCommonModuleTests.Run();
        if (failures == 0)
            System.Console.WriteLine("PASS CharacterCommonModuleTests");
        return failures == 0 ? 0 : 1;
    }
}
