using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Persistence;

/// <summary>
/// Translates MySQL provider exceptions into persistence abstraction exceptions.
/// </summary>
internal static class MySqlExceptionTranslator
{
    private const int DuplicateEntryErrorNumber = 1062;
    private const int ForeignKeyConstraintErrorNumber = 1452;

    public static Exception TranslateUserException(Exception exception)
    {
        var mysqlEx = FindMySqlException(exception);
        if (mysqlEx is null)
        {
            return exception;
        }

        if (mysqlEx.Number == DuplicateEntryErrorNumber)
        {
            if (mysqlEx.Message.Contains("UX_Users_Username", StringComparison.OrdinalIgnoreCase))
            {
                return new DuplicateEntryException(nameof(User.Username));
            }

            if (mysqlEx.Message.Contains("UX_Users_Email", StringComparison.OrdinalIgnoreCase))
            {
                return new DuplicateEntryException(nameof(User.Email));
            }
        }

        if (mysqlEx.Number == ForeignKeyConstraintErrorNumber)
        {
            return new InvalidOperationException("Referenced role does not exist.", exception);
        }

        return exception;
    }

    public static Exception TranslateSessionException(Exception exception)
    {
        var mysqlEx = FindMySqlException(exception);
        if (mysqlEx is not null &&
            (mysqlEx.Number == DuplicateEntryErrorNumber || mysqlEx.Number == ForeignKeyConstraintErrorNumber))
        {
            return new InvalidOperationException("Session key already exists or referenced user does not exist.", exception);
        }

        return exception;
    }

    private static MySqlException? FindMySqlException(Exception exception) =>
        exception switch
        {
            MySqlException mysqlEx => mysqlEx,
            DbUpdateException { InnerException: MySqlException innerMysqlEx } => innerMysqlEx,
            _ => null
        };
}
