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
    private const int RowIsReferencedErrorNumber = 1451;

    public static Exception TranslateCategoryException(Exception exception)
    {
        var mysqlEx = FindMySqlException(exception);
        if (mysqlEx?.Number == DuplicateEntryErrorNumber
            && mysqlEx.Message.Contains("UX_Categories_Name", StringComparison.OrdinalIgnoreCase))
        {
            return new DuplicateEntryException(nameof(Category.Name));
        }

        return exception;
    }

    public static Exception TranslateProductException(Exception exception)
    {
        var mysqlEx = FindMySqlException(exception);
        if (mysqlEx?.Number == ForeignKeyConstraintErrorNumber)
        {
            return new InvalidOperationException("Referenced category does not exist.", exception);
        }

        return exception;
    }

    /// <summary>Whether a delete failed because other rows still reference the row (MySQL error 1451).</summary>
    public static bool IsRowStillReferenced(Exception exception) =>
        FindMySqlException(exception)?.Number == RowIsReferencedErrorNumber;

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
