namespace Code.Utils.FloatUtils
{
public static class FloatExtensions
{
	public static int ToMilliseconds(this float timePerSeconds)
	{
		return (int)timePerSeconds * 1000;
	}
}
}