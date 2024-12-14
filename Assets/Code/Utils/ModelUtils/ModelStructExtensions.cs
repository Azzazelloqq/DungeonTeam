using UnityEngine;

namespace Code.Utils.ModelUtils
{
public static class ModelStructExtensions
{
	public static UnityEngine.Vector3 ToUnityVector(this ModelVector3 modelVector3)
	{
		return new UnityEngine.Vector3(modelVector3.X, modelVector3.Y, modelVector3.Z);
	}
	
	public static ModelVector3 ToModelVector(this Vector3 vector3)
	{
		return new ModelVector3(vector3.x, vector3.y, vector3.z);
	}
	
	public static UnityEngine.Vector2 ToUnityVector(this ModelVector2 modelVector2)
	{
		return new UnityEngine.Vector2(modelVector2.X, modelVector2.Y);
	}

	public static ModelVector2 ToModelVector(this Vector2 vector2)
	{
		return new ModelVector2(vector2.x, vector2.y);
	}
}
}