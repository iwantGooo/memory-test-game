public class GameConfig
{    
    private static GameConfig _instance = new GameConfig();
    private GameConfig() { }
    public static GameConfig Instance { get { return _instance; } }

    public float MemoryTime = 10f;
    public float GameTime = 15f;
    public float WhiteScreenTime = 3f;
}