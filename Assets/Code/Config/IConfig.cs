namespace Code.Config
{
public interface IConfig
{
	public T GetData<T>() where T : IConfigData;
}
}