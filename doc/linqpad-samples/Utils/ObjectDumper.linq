<Query Kind="Program">
  <Namespace>System.ComponentModel</Namespace>
</Query>

void Main()
{

}

public static class ObjectDumperExtensions
{
	public static string JsonLDDump(this object obj) => ObjectDumper.Dump(obj);
}

// thanks: https://stackoverflow.com/a/42264037
public class ObjectDumper
{
	public static string Dump(object obj)
	{
		return new ObjectDumper().DumpObject(obj);
	}

	private readonly StringBuilder _dumpBuilder = new StringBuilder();

	private string DumpObject(object obj)
	{
		DumpObject(obj, 0);
		return _dumpBuilder.ToString();
	}

	private void DumpObject(object obj, int nestingLevel = 0)
	{
		var nestingSpaces = new String('\t', nestingLevel); //"".PadLeft(nestingLevel * 4);

		if (obj == null)
		{
			_dumpBuilder.AppendFormat("null", nestingSpaces);
		}
		else if (obj is string || obj.GetType().IsPrimitive)
		{
			_dumpBuilder.AppendFormat("{1}", nestingSpaces, obj.ToString().PadRight(8));
		}
		else if (ImplementsDictionary(obj.GetType()))
		{
			using var e = ((dynamic)obj).GetEnumerator();
			var enumerator = (IEnumerator)e;
			while (enumerator.MoveNext())
			{
				dynamic p = enumerator.Current;

				var key = p.Key;
				var value = p.Value;
				_dumpBuilder.AppendFormat("\n{0}{1}", nestingSpaces, key.PadRight(10), value != null ? value.GetType().ToString() : "<null>");
				DumpObject(value, nestingLevel + 1);
			}
		}
		else if (obj is IEnumerable)
		{
			foreach (dynamic p in obj as IEnumerable)
			{
				DumpObject(p, nestingLevel);
				DumpObject("\n", nestingLevel);
				DumpObject("---", nestingLevel);
			}
		}
		else
		{
			foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
			{
				string name = descriptor.Name;
				object value = descriptor.GetValue(obj);

				_dumpBuilder.AppendFormat("{0}{1}\n", nestingSpaces, name.PadRight(10), value != null ? value.GetType().ToString() : "<null>");
				DumpObject(value, nestingLevel + 1);
			}
		}
	}

	private bool ImplementsDictionary(Type t) => t.GetInterfaces().Any(i => i.Name.Contains("IDictionary"));
}
