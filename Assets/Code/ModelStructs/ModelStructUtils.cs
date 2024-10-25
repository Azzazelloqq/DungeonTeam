namespace Code.ModelStructs
{
public static class ModelStructUtils
{
	public static UnityEngine.Vector3 ToUnityVector(this Vector3 vector3)
	{
		return new UnityEngine.Vector3(vector3.X, vector3.Y, vector3.Z);
	}
	
	public static Vector3 ToModelVector(this UnityEngine.Vector3 vector3)
	{
		return new Vector3(vector3.x, vector3.y, vector3.z);
	}
}
}