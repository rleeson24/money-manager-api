using System.Collections;
using System.Data;
using System.Data.Common;

namespace MoneyManager.Data.Tests.Helpers;

/// <summary>
/// DbDataReader test double backed by a single row dictionary.
/// </summary>
internal sealed class DictionaryDbDataReader : DbDataReader
{
	private readonly Dictionary<string, int> _ordinals;
	private readonly object?[] _values;
	private bool _closed;
	private bool _read;

	public DictionaryDbDataReader(IReadOnlyDictionary<string, object?> row)
	{
		_ordinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		_values = new object?[row.Count];
		var index = 0;
		foreach (var (name, value) in row)
		{
			_ordinals[name] = index;
			_values[index] = value;
			index++;
		}
	}

	public static DbDataReader Create(IReadOnlyDictionary<string, object?> row) =>
		new DictionaryDbDataReader(row);

	public override int Depth => 0;

	public override int FieldCount => _values.Length;

	public override bool HasRows => _values.Length > 0;

	public override bool IsClosed => _closed;

	public override int RecordsAffected => 0;

	public override object this[int ordinal] => GetValue(ordinal);

	public override object this[string name] => GetValue(GetOrdinal(name));

	public override void Close() => _closed = true;

	public override bool GetBoolean(int ordinal) => (bool)_values[ordinal]!;

	public override byte GetByte(int ordinal) => (byte)_values[ordinal]!;

	public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) =>
		throw new NotSupportedException();

	public override char GetChar(int ordinal) => (char)_values[ordinal]!;

	public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) =>
		throw new NotSupportedException();

	public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;

	public override DateTime GetDateTime(int ordinal) => (DateTime)_values[ordinal]!;

	public override decimal GetDecimal(int ordinal) => (decimal)_values[ordinal]!;

	public override double GetDouble(int ordinal) => (double)_values[ordinal]!;

	public override IEnumerator GetEnumerator() => throw new NotSupportedException();

	public override Type GetFieldType(int ordinal)
	{
		var value = _values[ordinal];
		if (value is null or DBNull)
			return typeof(object);
		return value.GetType();
	}

	public override float GetFloat(int ordinal) => (float)_values[ordinal]!;

	public override Guid GetGuid(int ordinal) => (Guid)_values[ordinal]!;

	public override short GetInt16(int ordinal) => (short)_values[ordinal]!;

	public override int GetInt32(int ordinal) => (int)_values[ordinal]!;

	public override long GetInt64(int ordinal) => (long)_values[ordinal]!;

	public override string GetName(int ordinal) =>
		_ordinals.First(kv => kv.Value == ordinal).Key;

	public override int GetOrdinal(string name) => _ordinals[name];

	public override string GetString(int ordinal) => (string)_values[ordinal]!;

	public override object GetValue(int ordinal) =>
		_values[ordinal] is null or DBNull ? DBNull.Value : _values[ordinal]!;

	public override int GetValues(object[] values)
	{
		var count = Math.Min(values.Length, _values.Length);
		for (var i = 0; i < count; i++)
			values[i] = GetValue(i);
		return count;
	}

	public override bool IsDBNull(int ordinal) =>
		_values[ordinal] is null or DBNull;

	public override bool NextResult() => false;

	public override bool Read()
	{
		if (_closed)
			return false;
		if (_read)
			return false;
		_read = true;
		return _values.Length > 0;
	}
}
