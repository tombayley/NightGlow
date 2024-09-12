namespace NightGlow.Models;

public class Setting
{

    public uint Min;
    public uint Max;
    public uint Current;

    public Setting()
    {
        Min = 0;
        Max = 0;
        Current = 0;
    }

    public Setting(uint min, uint max, uint current)
    {
        Min = min;
        Max = max;
        Current = current;
    }

}
