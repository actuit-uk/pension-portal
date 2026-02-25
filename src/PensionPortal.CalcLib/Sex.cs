namespace PensionPortal.CalcLib;

/// <summary>
/// Biological sex for GMP calculation purposes.
/// GMP payable age differs: 60 for female, 65 for male.
/// </summary>
public enum Sex
{
    /// <summary>Male — GMP payable at age 65.</summary>
    Male,

    /// <summary>Female — GMP payable at age 60.</summary>
    Female
}
