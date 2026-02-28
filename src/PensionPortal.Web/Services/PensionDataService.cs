using System.Data;
using Microsoft.Data.SqlClient;

namespace PensionPortal.Web.Services;

/// <summary>
/// Data access for pension-standard (PensionStandardv02) tables.
/// Uses inline parameterised SQL to keep the pension-standard DB clean of app-specific sprocs.
/// </summary>
public class PensionDataService
{
    private readonly string _connectionString;

    public PensionDataService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PensionStandard")
            ?? throw new InvalidOperationException("Connection string 'PensionStandard' not configured.");
    }

    /// <summary>Returns all schemes with member counts.</summary>
    public DataTable GetSchemes()
    {
        const string sql = """
            SELECT s.scheme_id, s.scheme_name, s.scheme_type,
                   COUNT(m.membership_id) AS member_count
            FROM dbo.scheme s
            LEFT JOIN dbo.member m ON m.scheme_id = s.scheme_id
            GROUP BY s.scheme_id, s.scheme_name, s.scheme_type
            ORDER BY s.scheme_id
            """;
        return ExecuteQuery(sql);
    }

    /// <summary>Returns schemes filtered by IDs, with member counts.</summary>
    public DataTable GetSchemes(string[] schemeIds)
    {
        if (schemeIds.Length == 0) return new DataTable();

        // Build parameterised IN clause
        var paramNames = schemeIds.Select((_, i) => $"@p{i}").ToArray();
        var sql = $"""
            SELECT s.scheme_id, s.scheme_name, s.scheme_type,
                   COUNT(m.membership_id) AS member_count
            FROM dbo.scheme s
            LEFT JOIN dbo.member m ON m.scheme_id = s.scheme_id
            WHERE s.scheme_id IN ({string.Join(", ", paramNames)})
            GROUP BY s.scheme_id, s.scheme_name, s.scheme_type
            ORDER BY s.scheme_id
            """;

        var parameters = schemeIds
            .Select((id, i) => new SqlParameter($"@p{i}", id))
            .ToArray();

        return ExecuteQuery(sql, parameters);
    }

    /// <summary>Returns schemes that a specific person belongs to.</summary>
    public DataTable GetSchemesForPerson(int personId)
    {
        const string sql = """
            SELECT DISTINCT s.scheme_id, s.scheme_name, s.scheme_type,
                   COUNT(m2.membership_id) AS member_count
            FROM dbo.member m
            JOIN dbo.scheme s ON s.scheme_id = m.scheme_id
            LEFT JOIN dbo.member m2 ON m2.scheme_id = s.scheme_id
            WHERE m.person_id = @PersonId
            GROUP BY s.scheme_id, s.scheme_name, s.scheme_type
            ORDER BY s.scheme_id
            """;
        return ExecuteQuery(sql, new SqlParameter("@PersonId", personId));
    }

    /// <summary>Returns scheme detail (single row).</summary>
    public DataTable GetSchemeDetail(string schemeId)
    {
        const string sql = """
            SELECT scheme_id, scheme_name, scheme_type,
                   contracted_out_start, contracted_out_end,
                   normal_retirement_age, trustees, actuary, administrator
            FROM dbo.scheme
            WHERE scheme_id = @SchemeId
            """;
        return ExecuteQuery(sql, new SqlParameter("@SchemeId", schemeId));
    }

    /// <summary>Returns sections for a scheme.</summary>
    public DataTable GetSections(string schemeId)
    {
        const string sql = """
            SELECT section_id, section_name, section_type,
                   male_nra, female_nra, accrual_denominator,
                   increase_method, pip_method, anti_franking_applies,
                   default_revaluation, status
            FROM dbo.section
            WHERE scheme_id = @SchemeId
            ORDER BY section_id
            """;
        return ExecuteQuery(sql, new SqlParameter("@SchemeId", schemeId));
    }

    /// <summary>Returns members for a scheme with person details.</summary>
    public DataTable GetMembers(string schemeId)
    {
        const string sql = """
            SELECT m.membership_id, m.scheme_id, m.section_id,
                   p.person_id, p.forename, p.surname, p.gender, p.date_of_birth,
                   m.date_of_joining, m.date_of_leaving, m.section,
                   sec.section_name
            FROM dbo.member m
            JOIN dbo.person p ON p.person_id = m.person_id
            LEFT JOIN dbo.section sec ON sec.section_id = m.section_id
            WHERE m.scheme_id = @SchemeId
            ORDER BY p.surname, p.forename
            """;
        return ExecuteQuery(sql, new SqlParameter("@SchemeId", schemeId));
    }

    /// <summary>Returns full member detail including person, section, and GMP records.</summary>
    public DataTable GetMemberDetail(int membershipId)
    {
        const string sql = """
            SELECT m.membership_id, m.scheme_id, m.section_id,
                   m.date_of_joining, m.date_of_leaving,
                   m.normal_retirement_age, m.member_type,
                   p.person_id, p.forename, p.surname, p.gender,
                   p.date_of_birth, p.nino,
                   s.scheme_name, s.scheme_type,
                   sec.section_name, sec.male_nra, sec.female_nra,
                   sec.accrual_denominator, sec.increase_method, sec.pip_method,
                   sec.anti_franking_applies, sec.default_revaluation
            FROM dbo.member m
            JOIN dbo.person p ON p.person_id = m.person_id
            JOIN dbo.scheme s ON s.scheme_id = m.scheme_id
            LEFT JOIN dbo.section sec ON sec.section_id = m.section_id
            WHERE m.membership_id = @MembershipId
            """;
        return ExecuteQuery(sql, new SqlParameter("@MembershipId", membershipId));
    }

    /// <summary>Returns GMP records for a membership.</summary>
    public DataTable GetGmpRecords(int membershipId)
    {
        const string sql = """
            SELECT gmp_id, membership_id, gmp_source,
                   accrual_start_date, accrual_end_date,
                   revaluation_basis,
                   gmp_amount_male, gmp_amount_female,
                   equalisation_status
            FROM dbo.gmp
            WHERE membership_id = @MembershipId
            ORDER BY accrual_start_date
            """;
        return ExecuteQuery(sql, new SqlParameter("@MembershipId", membershipId));
    }

    /// <summary>Returns members that have at least one GMP record, with key GMP fields.</summary>
    public DataTable GetMembersWithGmp()
    {
        const string sql = """
            SELECT m.membership_id, m.scheme_id, m.date_of_joining, m.date_of_leaving,
                   p.person_id, p.forename, p.surname, p.gender, p.date_of_birth,
                   s.scheme_name,
                   MIN(g.accrual_start_date) AS accrual_start_date,
                   MAX(g.accrual_end_date) AS accrual_end_date
            FROM dbo.member m
            JOIN dbo.person p ON p.person_id = m.person_id
            JOIN dbo.scheme s ON s.scheme_id = m.scheme_id
            JOIN dbo.gmp g ON g.membership_id = m.membership_id
            GROUP BY m.membership_id, m.scheme_id, m.date_of_joining, m.date_of_leaving,
                     p.person_id, p.forename, p.surname, p.gender, p.date_of_birth,
                     s.scheme_name
            ORDER BY p.surname, p.forename
            """;
        return ExecuteQuery(sql);
    }

    private DataTable ExecuteQuery(string sql, params SqlParameter[] parameters)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddRange(parameters);
        conn.Open();

        var table = new DataTable();
        using var reader = cmd.ExecuteReader();
        table.Load(reader);
        return table;
    }
}
