namespace Code.Utils.ValuesUtils
{
public static class TimeExtensions
{
	public static int ToMilliseconds(this float timePerSeconds)
	{
		return (int)(timePerSeconds * 1000);
	}

	public static float ToSeconds(this int timePerMilliseconds)
	{
		return (float)timePerMilliseconds / 1000;
	}
}
}