using System.Data;
using JetBrains.Annotations;
using Microsoft.Data.SqlClient;

namespace Keel.Infra.SqlServer;

[UsedImplicitly]
public class DbDirectAccessBuilder(SqlCommand command)
{
    public enum EExecMode
    {
        DataRow,
        DataTable,
        DataSet,
        PrimitiveValue,
    }

    public EExecMode Mode { get; private set; } = EExecMode.DataTable;

    public DbDirectAccessBuilder SetExecutionByReturnType<TResult>()
    {
        Mode = IdentifyExecMode<TResult>();

        return this;
    }

    public DbDirectAccessBuilder SetExecutionMode(EExecMode mode)
    {
        Mode = mode;

        return this;
    }

    public DbDirectAccessBuilder ForCommand(string command1, CommandType commandType)
    {
        command.CommandText = command1;
        command.CommandType = commandType;

        return this;
    }

    public DbDirectAccessBuilder ForText(string command1)
    {
        command.CommandText = command1;
        command.CommandType = CommandType.Text;

        return this;
    }

    public DbDirectAccessBuilder ForStoredProc(string command1)
    {
        command.CommandText = command1;
        command.CommandType = CommandType.StoredProcedure;

        return this;
    }

    public DbDirectAccessBuilder WithTimeout(int timeout)
    {
        command.CommandTimeout = timeout;

        return this;
    }

    public DbDirectAccessBuilder AddByte(string name, byte value)
    {
        command.Parameters.Add(
            new SqlParameter().Set(name, SqlDbType.TinyInt, value));

        return this;
    }

    public DbDirectAccessBuilder AddInt(string name, int? value)
    {
        command.Parameters.Add(
            new SqlParameter().Set(name, SqlDbType.Int, value));

        return this;
    }

    public DbDirectAccessBuilder AddEnum(string name, Enum value)
    {
        command.Parameters.Add(
            new SqlParameter().Set(name, SqlDbType.Int, value));

        return this;
    }

    public DbDirectAccessBuilder AddString(string name, string value)
    {
        command.Parameters.Add(
            new SqlParameter().Set(name, SqlDbType.VarChar, value));

        return this;
    }

    public DbDirectAccessBuilder AddParameters(params SqlParameter[] parameters)
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