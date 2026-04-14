using System.Data;
using JetBrains.Annotations;
using Microsoft.Data.SqlClient;

namespace Keel.Infra.Db.SqlServer.Access;

[UsedImplicitly]
public class SqlDirectAccessBuilder(SqlCommand command)
{
    public enum EExecMode
    {
        DataRow,
        DataTable,
        DataSet,
        PrimitiveValue,
    }

    public EExecMode Mode { get; private set; } = EExecMode.DataTable;

    public SqlDirectAccessBuilder SetExecutionByReturnType<TResult>()
    {
        Mode = IdentifyExecMode<TResult>();

        return this;
    }

    public SqlDirectAccessBuilder SetExecutionMode(EExecMode mode)
    {
        Mode = mode;

        return this;
    }

    public SqlDirectAccessBuilder ForCommand(string command1, CommandType commandType)
    {
        command.CommandText = command1;
        command.CommandType = commandType;

        return this;
    }

    public SqlDirectAccessBuilder ForText(string command1)
    {
        command.CommandText = command1;
        command.CommandType = CommandType.Text;

        return this;
    }

    public SqlDirectAccessBuilder ForStoredProc(string command1)
    {
        command.CommandText = command1;
        command.CommandType = CommandType.StoredProcedure;

        return this;
    }

    public SqlDirectAccessBuilder WithTimeout(int timeout)
    {
        command.CommandTimeout = timeout;

        return this;
    }

    public SqlDirectAccessBuilder AddByte(string name, byte value)
    {
        command.Parameters.Add(
            new SqlParameter().Set(name, SqlDbType.TinyInt, value));

        return this;
    }

    public SqlDirectAccessBuilder AddInt(string name, int? value)
    {
        command.Parameters.Add(
            new SqlParameter().Set(name, SqlDbType.Int, value));

        return this;
    }

    public SqlDirectAccessBuilder AddEnum(string name, Enum value)
    {
        command.Parameters.Add(
            new SqlParameter().Set(name, SqlDbType.Int, value));

        return this;
    }

    public SqlDirectAccessBuilder AddString(string name, string value)
    {
        command.Parameters.Add(
            new SqlParameter().Set(name, SqlDbType.VarChar, value));

        return this;
    }

    public SqlDirectAccessBuilder AddParameters(params SqlParameter[] parameters)
    {
        command.Parameters.AddRange(parameters);

        return this;
    }

    private static EExecMode IdentifyExecMode<TResult>()
    {
        var type = typeof(TResult);
        if (type == typeof(DataRow))
        {
            return EExecMode.DataRow;
        }

        if (type == typeof(DataTable))
        {
            return EExecMode.DataTable;
        }

        if (type == typeof(DataSet))
        {
            return EExecMode.DataSet;
        }

        return EExecMode.PrimitiveValue;
    }
}