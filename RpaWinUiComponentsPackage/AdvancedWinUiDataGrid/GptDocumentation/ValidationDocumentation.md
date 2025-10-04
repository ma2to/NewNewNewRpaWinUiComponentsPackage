# KOMPLETNÁ AKTUALIZOVANÁ ŠPECIFIKÁCIA VALIDAČNÉHO SYSTÉMU

## 🏗️ ARCHITEKTONICKÉ PRINCÍPY

### Clean Architecture + Command Pattern
- **Facade Layer**: `IAdvancedDataGridFacade` (public API)
- **Application Layer**: Command handlers, services (internal)
- **Core Layer**: Domain entities, validation rules (internal)
- **Infrastructure Layer**: Data access, external services (internal)
- **Hybrid Internal DI + Functional/OOP**: Kombinuje dependency injection s funkcionálnym programovaním

### SOLID Principles
- **Single Responsibility**: Každé validačné pravidlo má jednu zodpovednosť
- **Open/Closed**: Rozšíriteľné pre nové typy validácií bez zmeny existujúceho kódu
- **Liskov Substitution**: Všetky validation rules implementujú `IValidationRule`
- **Interface Segregation**: Špecializované interfaces pre rôzne typy validácií
- **Dependency Inversion**: Facade závislí od abstrakcií, nie konkrétnych implementácií

### Architectural Principles Maintained
- **Clean Architecture**: Commands v Core layer, processing v Application layer
- **Hybrid DI**: Command factory methods s dependency injection support
- **Functional/OOP**: Immutable commands + encapsulated behavior
- **SOLID**: Single responsibility pre každý command type
- **LINQ Optimization**: Lazy evaluation, parallel processing, streaming where beneficial
- **Performance**: Object pooling, atomic operations, minimal allocations
- **Thread Safety**: Immutable commands, atomic RowNumber updates
- **Internal DI Registration**: Všetky validation časti budú registrované v InternalServiceRegistration.cs

## 🔄 BACKUP STRATEGY & IMPLEMENTATION APPROACH

### 1. Backup Strategy
- Vytvoriť .oldbackup_timestamp súbory pre všetky modifikované súbory
- Úplne nahradiť staré implementácie - **ŽIADNA backward compatibility**
- Zachovať DI registrácie a interface contracts

### 2. Implementation Replacement
- Kompletný refaktoring s command pattern a LINQ optimizations
- Bez backward compatibility ale s preservation DI architektúry
- Optimalizované a bezpečné a stabilné riešenie

## 🎯 8 TYPOV VALIDAČNÝCH PRAVIDIEL

### 1. **RequiredFieldValidationRule**
```csharp
// FLEXIBLE RULE CREATION - nie hardcoded factory methods
var rule = new RequiredFieldValidationRule
{
    ColumnName = "Name",
    ErrorMessage = "Name is required",
    ValidationTimeout = TimeSpan.FromSeconds(2), // default timeout
    IsEnabled = true
};

// 🔄 AUTOMATICKÉ REVALIDOVANIE:
// Pri zmene hodnoty v stĺpci "Name" sa automaticky spustí revalidácia tohto pravidla
// Toto platí pre VŠETKY validačné pravidlá, nie len ako príklad
```

### 2. **RangeValidationRule**
```csharp
var rule = new RangeValidationRule
{
    ColumnName = "Age",
    MinValue = 18,
    MaxValue = 65,
    ErrorMessage = "Age must be between 18 and 65",
    ValidationTimeout = TimeSpan.FromSeconds(2),

    // 🔄 AUTOMATICKÉ REVALIDOVANIE:
    // Pri zmene hodnoty v stĺpci "Age" sa automaticky revaliduje
    DependentColumns = new[] { "Age" }
};
```

### 3. **RegexValidationRule**
```csharp
var rule = new RegexValidationRule
{
    ColumnName = "Email",
    Pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
    ErrorMessage = "Invalid email format",
    ValidationTimeout = TimeSpan.FromSeconds(2),

    // 🔄 AUTOMATICKÉ REVALIDOVANIE pre VŠETKY pravidlá
    DependentColumns = new[] { "Email" }
};
```

### 4. **CustomFunctionValidationRule**
```csharp
var rule = new CustomFunctionValidationRule
{
    ColumnName = "Password",
    ValidationFunction = (value, row, context) =>
    {
        var password = value?.ToString();
        return !string.IsNullOrEmpty(password) &&
               password.Length >= 8 &&
               password.Any(char.IsUpper) &&
               password.Any(char.IsDigit);
    },
    ErrorMessage = "Password must be at least 8 characters with uppercase and digit",
    ValidationTimeout = TimeSpan.FromSeconds(2),

    // 🔄 AUTOMATICKÉ REVALIDOVANIE pre VŠETKY pravidlá
    DependentColumns = new[] { "Password" }
};
```

### 5. **CrossColumnValidationRule**
```csharp
var rule = new CrossColumnValidationRule
{
    PrimaryColumn = "StartDate",
    ErrorMessage = "Start date must be before end date",
    ValidationTimeout = TimeSpan.FromSeconds(2),

    // 🔄 AUTOMATICKÉ REVALIDOVANIE pre VŠETKY pravidlá
    // Pri zmene AKÉHOKOĽVEK dependent column sa revaliduje
    DependentColumns = new[] { "StartDate", "EndDate" },

    ValidationFunction = (primaryValue, row, context) =>
    {
        if (DateTime.TryParse(primaryValue?.ToString(), out var startDate) &&
            DateTime.TryParse(row.GetValueOrDefault("EndDate")?.ToString(), out var endDate))
        {
            return startDate <= endDate;
        }
        return true; // Ak nie sú valid dates, nech to rieši iné pravidlo
    }
};
```

### 6. **ConditionalValidationRule**
```csharp
var rule = new ConditionalValidationRule
{
    ColumnName = "DriversLicense",
    ErrorMessage = "Driver's license required for drivers",
    ValidationTimeout = TimeSpan.FromSeconds(2),

    // 🔄 AUTOMATICKÉ REVALIDOVANIE pre VŠETKY pravidlá
    DependentColumns = new[] { "DriversLicense", "JobPosition" },

    Condition = (row, context) =>
        row.GetValueOrDefault("JobPosition")?.ToString() == "Driver",

    ValidationFunction = (value, row, context) =>
        !string.IsNullOrWhiteSpace(value?.ToString())
};
```

### 7. **AsyncValidationRule**
```csharp
var rule = new AsyncValidationRule
{
    ColumnName = "Username",
    ErrorMessage = "Username already exists",
    ValidationTimeout = TimeSpan.FromSeconds(2),

    // 🔄 AUTOMATICKÉ REVALIDOVANIE pre VŠETKY pravidlá
    DependentColumns = new[] { "Username" },

    AsyncValidationFunction = async (value, row, context, cancellationToken) =>
    {
        var username = value?.ToString();
        if (string.IsNullOrEmpty(username)) return true;

        // External API call with timeout
        using var cts = cancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ValidationTimeout);

        return await context.UserService.IsUsernameAvailableAsync(username, cts.Token);
    }
};
```

### 8. **GroupValidationRule**
```csharp
var rule = new GroupValidationRule
{
    GroupName = "ContactInfo",
    ErrorMessage = "At least one contact method required",
    ValidationTimeout = TimeSpan.FromSeconds(2),

    // 🔄 AUTOMATICKÉ REVALIDOVANIE pre VŠETKY pravidlá
    DependentColumns = new[] { "Email", "Phone", "Address" },

    ValidationFunction = (row, context) =>
    {
        var email = row.GetValueOrDefault("Email")?.ToString();
        var phone = row.GetValueOrDefault("Phone")?.ToString();
        var address = row.GetValueOrDefault("Address")?.ToString();

        return !string.IsNullOrWhiteSpace(email) ||
               !string.IsNullOrWhiteSpace(phone) ||
               !string.IsNullOrWhiteSpace(address);
    }
};
```

## ⚡ PRIORITY-BASED VALIDATION

### **Priority-based validation príklady:**

```csharp
// Logické poradie pravidiel pre jeden stĺpec
var emailRequiredRule = new ValidationRule("Email", v => v != null, "Email required", Priority: 1, RuleName: "EmailRequired");
var emailLengthRule = new ValidationRule("Email", v => v.Length > 5, "Email too short", Priority: 2, RuleName: "EmailLength");
var emailFormatRule = new ValidationRule("Email", v => IsValidFormat(v), "Invalid format", Priority: 3, RuleName: "EmailFormat");

// Cross-column s prioritou
var prioritizedCrossRule = new CrossColumnValidationRule(
    ["StartDate", "EndDate"],
    row => ValidateDateRange(row),
    "End date must be after start date",
    Priority: 5,
    RuleName: "DateRangeRule"
);
```

##### **1. Single Cell Validation**
```csharp
public record ValidationRule(
    string ColumnName,
    Func<object?, bool> Validator,
    string ErrorMessage,
    ValidationSeverity Severity = ValidationSeverity.Error,
    int Priority = 0)

// Example: Age validation with multiple rules per column
var validationConfig = new ValidationConfiguration
{
    ColumnValidationRules = new()
    {
        ["Age"] = new List<ValidationRule>
        {
            new() { RuleName = "Required", Validator = v => v != null, ErrorMessage = "Age is required" },
            new() { RuleName = "Range", Validator = v => (int)v >= 18 && (int)v <= 120, ErrorMessage = "Age must be 18-120" },
            new() { RuleName = "Business", Validator = v => IsValidAge((int)v), ErrorMessage = "Invalid business age rule" }
        },
        ["Email"] = new List<ValidationRule>
        {
            new()
            {
                RuleName = "Format",
                Validator = email => IsValidEmail(email?.ToString()),
                ErrorMessage = "Invalid email format",
                Severity = ValidationSeverity.Error
            },
            new()
            {
                RuleName = "Domain",
                Validator = email => IsValidDomain(email?.ToString()),
                ErrorMessage = "Email domain not allowed",
                Severity = ValidationSeverity.Warning
            }
        }
    }
};
```

##### **2. Multi-Cell Same Row Validation (Cross-Cell)**
```csharp
public record CrossCellValidationRule(
    IReadOnlyList<string> ColumnNames,
    Func<IReadOnlyDictionary<string, object?>, ValidationResult> Validator,
    string ErrorMessage,
    ValidationSeverity Severity = ValidationSeverity.Error)

// Example: Start/End date validation
var dateRangeRule = new CrossCellValidationRule(
    new[] { "StartDate", "EndDate" },
    row => {
        var start = (DateTime?)row["StartDate"];
        var end = (DateTime?)row["EndDate"];
        return start <= end
            ? ValidationResult.Success()
            : ValidationResult.Error("End date must be after start date");
    },
    "Invalid date range");

// CROSS-COLUMN VALIDATIONS
CrossColumnRules = new()
{
    new()
    {
        RuleName = "AgeEmail",
        Validator = row => ValidateAgeEmailRule(row),
        ErrorMessage = "If Age > 18, Email must be provided",
        DependentColumns = new[] { "Age", "Email" },
        PrimaryColumn = "Age"
    }
}
```

##### **3. Cross-Row Validation**
```csharp
public record CrossRowValidationRule(
    Func<IReadOnlyList<IReadOnlyDictionary<string, object?>>,
         IReadOnlyList<ValidationResult>> Validator,
    string ErrorMessage)

// CROSS-ROW VALIDATIONS
CrossRowRules = new()
{
    // Example: Unique email validation
    new CrossRowValidationRule(
        rows => {
            var results = new List<ValidationResult>();
            var emails = new HashSet<string>();

            for (int i = 0; i < rows.Count; i++)
            {
                var email = rows[i]["Email"]?.ToString();
                if (!string.IsNullOrEmpty(email))
                {
                    if (emails.Contains(email))
                    {
                        results.Add(ValidationResult.ErrorForRow(i, "Email must be unique"));
                    }
                    else
                    {
                        emails.Add(email);
                        results.Add(ValidationResult.Success());
                    }
                }
                else
                {
                    results.Add(ValidationResult.Success());
                }
            }
            return results;
        },
        "Duplicate email addresses found"),

    new()
    {
        RuleName = "TotalSum",
        Validator = rows => ValidateTotalSum(rows),
        ErrorMessage = "Sum of Amount column must equal Total in last row",
        AffectedColumns = new[] { "Amount", "Total" }
    }
}
```

##### **4. Cross-Column Validation**
```csharp
public record CrossColumnValidationRule(
    IReadOnlyList<string> ColumnNames,
    Func<IReadOnlyList<IReadOnlyDictionary<string, object?>>,
         IReadOnlyList<string>, ValidationResult> Validator,
    string ErrorMessage)

// Example: Sum validation across columns
var budgetSumRule = new CrossColumnValidationRule(
    new[] { "Q1Budget", "Q2Budget", "Q3Budget", "Q4Budget", "TotalBudget" },
    (rows, columns) => {
        foreach (var row in rows)
        {
            var q1 = Convert.ToDecimal(row["Q1Budget"] ?? 0);
            var q2 = Convert.ToDecimal(row["Q2Budget"] ?? 0);
            var q3 = Convert.ToDecimal(row["Q3Budget"] ?? 0);
            var q4 = Convert.ToDecimal(row["Q4Budget"] ?? 0);
            var total = Convert.ToDecimal(row["TotalBudget"] ?? 0);

            if (Math.Abs((q1 + q2 + q3 + q4) - total) > 0.01m)
            {
                return ValidationResult.Error($"Total budget mismatch");
            }
        }
        return ValidationResult.Success();
    },
    "Quarterly budget totals don't match");
```

##### **5. Cross-Row Cross-Column Validation**
```csharp
public record ComplexValidationRule(
    Func<IReadOnlyList<IReadOnlyDictionary<string, object?>>, ValidationResult> Validator,
    string ErrorMessage,
    ValidationSeverity Severity = ValidationSeverity.Error)

// Example: Department budget limits
var departmentBudgetRule = new ComplexValidationRule(
    rows => {
        var departmentTotals = rows
            .GroupBy(r => r["Department"]?.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(r => Convert.ToDecimal(r["Budget"] ?? 0)));

        foreach (var dept in departmentTotals)
        {
            if (dept.Value > 1_000_000) // 1M limit
            {
                return ValidationResult.Error($"Department {dept.Key} exceeds budget limit");
            }
        }
        return ValidationResult.Success();
    },
    "Department budget limits exceeded");

// DATASET VALIDATIONS
DatasetRules = new()
{
    departmentBudgetRule,
    new()
    {
        RuleName = "UniqueEmail",
        Validator = dataset => ValidateUniqueEmails(dataset),
        ErrorMessage = "Email addresses must be unique",
        InvolvedColumns = new[] { "Email" }
    }
}
```

##### **6. Conditional Validation Rules**
```csharp
public record ConditionalValidationRule(
    string ColumnName,
    Func<IReadOnlyDictionary<string, object?>, bool> Condition,
    ValidationRule ValidationRule,
    string ErrorMessage)

// Example: Validate phone only if contact method is phone
var conditionalPhoneRule = new ConditionalValidationRule(
    "Phone",
    row => row["ContactMethod"]?.ToString() == "Phone",
    new ValidationRule(
        "Phone",
        value => !string.IsNullOrEmpty(value?.ToString()) && IsValidPhone(value.ToString()),
        "Phone number is required when contact method is Phone"),
    "Conditional phone validation failed");

// Example: Validate manager approval for high amounts
var managerApprovalRule = new ConditionalValidationRule(
    "ManagerApproval",
    row => Convert.ToDecimal(row["Amount"] ?? 0) > 10000,
    new ValidationRule(
        "ManagerApproval",
        value => value is bool approved && approved,
        "Manager approval required for amounts over $10,000"),
    "High amount requires manager approval");
```

**🔧 Jednotné Validation Management API:**
```csharp
// JEDNO API pre všetky typy validácie:
public async Task<Result<bool>> AddValidationRuleAsync<T>(T rule) where T : IValidationRule

// Príklady použitia - jednotlivé pravidlá:
await dataGrid.AddValidationRuleAsync(emailRequiredRule);    // Single cell s Priority
await dataGrid.AddValidationRuleAsync(crossColumnRule);     // Cross-column s RuleName
await dataGrid.AddValidationRuleAsync(crossRowRule);        // Cross-row s RuleName

// Pridanie všetkých email pravidiel jednotlivo:
await dataGrid.AddValidationRuleAsync(emailRequiredRule);
await dataGrid.AddValidationRuleAsync(emailLengthRule);
await dataGrid.AddValidationRuleAsync(emailFormatRule);

// Príklad - definovanie a pridanie v jednom kroku:
var emailRules = new[]
{
    new ValidationRule("Email", v => v != null, "Email required", Priority: 1, RuleName: "EmailRequired"),
    new ValidationRule("Email", v => v.Length > 5, "Email too short", Priority: 2, RuleName: "EmailLength"),
    new ValidationRule("Email", v => IsValidFormat(v), "Invalid format", Priority: 3, RuleName: "EmailFormat")
};

foreach(var rule in emailRules)
{
    await dataGrid.AddValidationRuleAsync(rule);
}

// Remove validation rules:
await dataGrid.RemoveValidationRulesAsync("Age", "Email");           // Podľa stĺpcov
await dataGrid.RemoveValidationRuleAsync("EmailRequired");           // Podľa RuleName
await dataGrid.RemoveValidationRuleAsync("UniqueEmailRule");         // Podľa RuleName
await dataGrid.ClearAllValidationRulesAsync();                       // Všetky pravidlá
```

## 🔗 VALIDATION RULE GROUPS S LOGICKÝMI OPERÁTORMI

```csharp
// FLEXIBLE GROUP CREATION - nie hardcoded
var businessRulesGroup = new ValidationRuleGroup
{
    GroupName = "BusinessRules",
    LogicalOperator = ValidationLogicalOperator.And, // AND/OR/AndAlso/OrElse
    Rules = new List<IValidationRule>
    {
        emailRule,
        ageRule,
        salaryRule
    }
};

var emergencyGroup = new ValidationRuleGroup
{
    GroupName = "EmergencyContact",
    LogicalOperator = ValidationLogicalOperator.Or, // Aspoň jeden kontakt
    Rules = new List<IValidationRule>
    {
        phoneRule,
        emailRule,
        addressRule
    }
};

// Hierarchické groupy
var masterGroup = new ValidationRuleGroup
{
    GroupName = "AllValidations",
    LogicalOperator = ValidationLogicalOperator.AndAlso, // Short-circuit evaluation
    Rules = new List<IValidationRule>(),
    ChildGroups = new List<ValidationRuleGroup>
    {
        businessRulesGroup,
        emergencyGroup
    }
};
```

## 📋 COMMAND PATTERN PRE VALIDÁCIE

### AddValidationRuleCommand
```csharp
public sealed record AddValidationRuleCommand<T> where T : IValidationRule
{
    public required T Rule { get; init; }
    public ValidationPriority Priority { get; init; } = ValidationPriority.Normal;
    public bool EnableRealTimeValidation { get; init; } = true;
    public TimeSpan? CustomTimeout { get; init; }

    // Factory methods pre FLEXIBLE creation s DI support
    public static AddValidationRuleCommand<T> Create(T rule) => new() { Rule = rule };

    public static AddValidationRuleCommand<T> WithPriority(T rule, ValidationPriority priority) =>
        new() { Rule = rule, Priority = priority };

    // DI factory method
    public static AddValidationRuleCommand<T> CreateWithDI(T rule, IServiceProvider services) =>
        new() { Rule = rule };
}
```

### RemoveValidationRuleCommand
```csharp
public sealed record RemoveValidationRuleCommand
{
    public string? RuleName { get; init; }
    public string[]? ColumnNames { get; init; }
    public ValidationRuleType? RuleType { get; init; }
    public bool RemoveAll { get; init; }

    // FLEXIBLE factory methods s DI support
    public static RemoveValidationRuleCommand ByName(string ruleName) =>
        new() { RuleName = ruleName };

    public static RemoveValidationRuleCommand ByColumns(params string[] columnNames) =>
        new() { ColumnNames = columnNames };

    public static RemoveValidationRuleCommand ByType(ValidationRuleType ruleType) =>
        new() { RuleType = ruleType };

    public static RemoveValidationRuleCommand All() =>
        new() { RemoveAll = true };
}
```

### ValidateDataCommand
```csharp
public sealed record ValidateDataCommand
{
    public required IEnumerable<IReadOnlyDictionary<string, object?>> Data { get; init; }
    public ValidationStrategy Strategy { get; init; } = ValidationStrategy.Automatic;
    public bool OnlyNonEmptyRows { get; init; } = true;
    public bool OnlyFilteredRows { get; init; } = false;
    public IProgress<ValidationProgress>? ProgressReporter { get; init; }
    public cancellationToken cancellationToken { get; init; } = default;

    // FLEXIBLE factory methods s LINQ optimization
    public static ValidateDataCommand Create(IEnumerable<IReadOnlyDictionary<string, object?>> data) =>
        new() { Data = data };

    public static ValidateDataCommand WithStrategy(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        ValidationStrategy strategy) =>
        new() { Data = data, Strategy = strategy };

    // LINQ optimized factory
    public static ValidateDataCommand WithLINQOptimization(
        IEnumerable<IReadOnlyDictionary<string, object?>> data) =>
        new() { Data = data.AsParallel().Where(row => row.Values.Any(v => v != null)) };
}
```

#### **🗑️ Row Deletion Based on Validation**

```csharp
/// <summary>
/// PROFESSIONAL: Delete rows that meet specified validation criteria
/// ENTERPRISE: Batch operation with progress reporting and rollback support
/// </summary>
/// <param name="validationCriteria">Criteria for determining which rows to delete</param>
/// <param name="options">Deletion options including safety checks</param>
/// <returns>Result with deletion statistics</returns>
public async Task<Result<ValidationBasedDeleteResult>> DeleteRowsWithValidationAsync(
    ValidationDeletionCriteria validationCriteria,
    ValidationDeletionOptions? options = null,
    cancellationToken cancellationToken = default)

public record ValidationDeletionCriteria(
    ValidationDeletionMode Mode,
    IReadOnlyList<ValidationSeverity>? Severity = null,     // Zoznam závažností na zmazanie
    IReadOnlyList<string>? SpecificRuleNames = null,
    Func<IReadOnlyDictionary<string, object?>, bool>? CustomPredicate = null)

public enum ValidationDeletionMode
{
    DeleteInvalidRows,      // Delete rows that fail validation
    DeleteValidRows,        // Delete rows that pass validation
    DeleteByCustomRule,     // Delete based on custom predicate
    DeleteBySeverity,       // Delete rows with specific severity levels
    DeleteByRuleName,       // Delete rows failing specific named rules
    DeleteDuplicates        // Delete duplicate rows based on specified columns
}

// Príklady použitia:
// Zmaž riadky s Error a Warning
await dataGrid.DeleteRowsWithValidationAsync(new ValidationDeletionCriteria(
    Mode: ValidationDeletionMode.DeleteBySeverity,
    Severity: [ValidationSeverity.Error, ValidationSeverity.Warning]
));

// Zmaž riadky ktoré zlyhali na konkrétnych pravidlách
await dataGrid.DeleteRowsWithValidationAsync(new ValidationDeletionCriteria(
    Mode: ValidationDeletionMode.DeleteByRuleName,
    SpecificRuleNames: ["EmailRequired", "UniqueEmail"]
));

// Zmaž podľa vlastnej funkcie
await dataGrid.DeleteRowsWithValidationAsync(new ValidationDeletionCriteria(
    Mode: ValidationDeletionMode.DeleteByCustomRule,
    CustomPredicate: row => (int)(row["Age"] ?? 0) > 65
));

public record ValidationDeletionOptions(
    bool RequireConfirmation = true,        // Vyžaduj potvrdenie pred zmazaním iba pre UI mode pri headless to potvrdenie bude vzdy false.
    IProgress<ValidationDeletionProgress>? Progress = null  // Progress reporting
)

public record ValidationBasedDeleteResult(
    int TotalRowsEvaluated,
    int RowsDeleted,
    int RemainingRows,
    IReadOnlyList<ValidationError> ValidationErrors,
    TimeSpan OperationDuration)
```

**Usage Examples:**
```csharp
// Delete all rows with validation errors
var errorCriteria = new ValidationDeletionCriteria(
    ValidationDeletionMode.DeleteInvalidRows,
    Severity: [ValidationSeverity.Error]);

var result = await dataGrid.DeleteRowsWithValidationAsync(errorCriteria);

// Delete rows failing specific rules
var specificRuleCriteria = new ValidationDeletionCriteria(
    ValidationDeletionMode.DeleteByRuleName,
    SpecificRuleNames: new[] { "AgeValidation", "EmailValidation" });

// Delete with custom logic
var customCriteria = new ValidationDeletionCriteria(
    ValidationDeletionMode.DeleteByCustomRule,
    CustomPredicate: row => Convert.ToDecimal(row["Amount"] ?? 0) < 0);

var customResult = await dataGrid.DeleteRowsWithValidationAsync(
    customCriteria);
```

### **🔍 Duplicate Detection Validation Rule**

```csharp
/// <summary>
/// PROFESSIONAL: Advanced duplicate detection validation rule
/// ENTERPRISE: Multiple comparison strategies with performance optimization
/// </summary>
public sealed record DuplicateDetectionValidationRule : IValidationRule
{
    public required string RuleName { get; init; } = "DuplicateDetection";
    public required IReadOnlyList<string> ComparisonColumns { get; init; }
    public DuplicateComparisonStrategy Strategy { get; init; } = DuplicateComparisonStrategy.ExactMatch;
    public bool CaseSensitive { get; init; } = false;
    public bool TrimWhitespace { get; init; } = true;
    public StringComparison StringComparison { get; init; } = StringComparison.OrdinalIgnoreCase;
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
    public string ErrorMessage { get; init; } = "Duplicate record found";
    public TimeSpan ValidationTimeout { get; init; } = TimeSpan.FromSeconds(5);

    // Advanced comparison options
    public Func<object?, object?, bool>? CustomComparer { get; init; }
    public decimal? NumericTolerance { get; init; } // Pre numerické hodnoty
    public bool IgnoreEmptyValues { get; init; } = true;
}

public enum DuplicateComparisonStrategy
{
    ExactMatch,         // Presná zhoda všetkých stĺpcov
    FuzzyMatch,         // Fuzzy string matching
    NumericTolerance,   // Numerické hodnoty s toleranciou
    CustomComparison,   // Vlastná porovnávacia funkcia
    HashBased          // Hash-based comparison pre performance
}

// Príklady vytvorenia duplicate detection rules:

// 1. Exact match na Email a Phone
var exactDuplicateRule = new DuplicateDetectionValidationRule
{
    RuleName = "ExactEmailPhoneDuplicate",
    ComparisonColumns = ["Email", "Phone"],
    Strategy = DuplicateComparisonStrategy.ExactMatch,
    ErrorMessage = "Duplicate person found (same email and phone)"
};

// 2. Fuzzy match na Name s toleranciou
var fuzzyNameRule = new DuplicateDetectionValidationRule
{
    RuleName = "FuzzyNameDuplicate",
    ComparisonColumns = ["FirstName", "LastName"],
    Strategy = DuplicateComparisonStrategy.FuzzyMatch,
    CaseSensitive = false,
    TrimWhitespace = true,
    ErrorMessage = "Similar name already exists"
};

// 3. Numerická tolerancia pre finančné údaje
var amountDuplicateRule = new DuplicateDetectionValidationRule
{
    RuleName = "AmountDuplicate",
    ComparisonColumns = ["Date", "Amount", "AccountNumber"],
    Strategy = DuplicateComparisonStrategy.NumericTolerance,
    NumericTolerance = 0.01m,
    ErrorMessage = "Duplicate transaction detected"
};

// 4. Custom comparison logic
var customDuplicateRule = new DuplicateDetectionValidationRule
{
    RuleName = "CustomBusinessLogic",
    ComparisonColumns = ["CompanyName", "TaxNumber"],
    Strategy = DuplicateComparisonStrategy.CustomComparison,
    CustomComparer = (val1, val2) =>
    {
        // Custom business logic for comparison
        return NormalizeCompanyName(val1) == NormalizeCompanyName(val2);
    },
    ErrorMessage = "Company with same business identity exists"
};
```

### **🗑️ Duplicate Deletion Integration**

```csharp
/// <summary>
/// Extended validation criteria with duplicate handling
/// </summary>
public record ValidationDeletionCriteria(
    ValidationDeletionMode Mode,
    IReadOnlyList<ValidationSeverity>? Severity = null,
    IReadOnlyList<string>? SpecificRuleNames = null,
    Func<IReadOnlyDictionary<string, object?>, bool>? CustomPredicate = null,
    // New duplicate-specific properties
    DuplicateHandlingMode? DuplicateMode = null,
    IReadOnlyList<string>? DuplicateComparisonColumns = null,
    DuplicateRetentionStrategy? RetentionStrategy = null)

public enum DuplicateHandlingMode
{
    DeleteAllDuplicates,        // Zmaže všetky duplicity vrátane originálu
    KeepFirstDeleteRest,        // Zachová prvý, zmaže ostatné
    KeepLastDeleteRest,         // Zachová posledný, zmaže ostatné
    KeepBestDeleteRest,         // Zachová "najlepší" podľa kritérií
    MarkDuplicatesOnly          // Len označí duplicity, nezmaže
}

public enum DuplicateRetentionStrategy
{
    FirstOccurrence,            // Zachovaj prvý výskyt
    LastOccurrence,             // Zachovaj posledný výskyt
    MostComplete,               // Zachovaj riadok s najviac vyplnenými stĺpcami
    HighestValue,               // Zachovaj riadok s najvyššou hodnotou v specified column
    LowestValue,                // Zachovaj riadok s najnižšou hodnotou
    CustomSelection             // Vlastná logika výberu
}

// Príklady použitia duplicate deletion:

// 1. Zmaž všetky duplicity (aj originály)
await dataGrid.DeleteRowsWithValidationAsync(new ValidationDeletionCriteria(
    Mode: ValidationDeletionMode.DeleteDuplicates,
    DuplicateMode: DuplicateHandlingMode.DeleteAllDuplicates,
    DuplicateComparisonColumns: ["Email", "Phone"]
));

// 2. Zachovaj prvý, zmaž ostatné duplicity
await dataGrid.DeleteRowsWithValidationAsync(new ValidationDeletionCriteria(
    Mode: ValidationDeletionMode.DeleteDuplicates,
    DuplicateMode: DuplicateHandlingMode.KeepFirstDeleteRest,
    DuplicateComparisonColumns: ["Email"],
    RetentionStrategy: DuplicateRetentionStrategy.FirstOccurrence
));

// 3. Zachovaj najkomplexnejší záznam
await dataGrid.DeleteRowsWithValidationAsync(new ValidationDeletionCriteria(
    Mode: ValidationDeletionMode.DeleteDuplicates,
    DuplicateMode: DuplicateHandlingMode.KeepBestDeleteRest,
    DuplicateComparisonColumns: ["CompanyName", "TaxNumber"],
    RetentionStrategy: DuplicateRetentionStrategy.MostComplete
));

// 4. Kombinované mazanie - duplicity + validation errors
await dataGrid.DeleteRowsWithValidationAsync(new ValidationDeletionCriteria(
    Mode: ValidationDeletionMode.DeleteInvalidRows,
    Severity: [ValidationSeverity.Error],
    DuplicateMode: DuplicateHandlingMode.KeepFirstDeleteRest,
    DuplicateComparisonColumns: ["Email"]
));
```

### **📊 Enhanced Validation Results**

```csharp
public record ValidationBasedDeleteResult(
    int TotalRowsEvaluated,
    int RowsDeleted,
    int RemainingRows,
    IReadOnlyList<ValidationError> ValidationErrors,
    TimeSpan OperationDuration,
    // Enhanced duplicate-specific results
    DuplicateDetectionSummary? DuplicateSummary = null)

public record DuplicateDetectionSummary(
    int TotalDuplicateGroups,
    int TotalDuplicateRows,
    int DuplicatesDeleted,
    int OriginalsRetained,
    IReadOnlyList<DuplicateGroup> DuplicateGroups)

public record DuplicateGroup(
    IReadOnlyList<string> ComparisonColumns,
    IReadOnlyDictionary<string, object?> GroupKey,
    IReadOnlyList<int> RowIndices,
    int RetainedRowIndex,
    IReadOnlyList<int> DeletedRowIndices)

// Usage s enhanced results:
var result = await dataGrid.DeleteRowsWithValidationAsync(duplicateCriteria);

if (result.IsSuccess && result.Value.DuplicateSummary != null)
{
    var summary = result.Value.DuplicateSummary;
    Console.WriteLine($"Found {summary.TotalDuplicateGroups} duplicate groups");
    Console.WriteLine($"Deleted {summary.DuplicatesDeleted} duplicate rows");
    Console.WriteLine($"Retained {summary.OriginalsRetained} original rows");

    foreach (var group in summary.DuplicateGroups)
    {
        Console.WriteLine($"Group: {string.Join(", ", group.GroupKey.Select(kv => $"{kv.Key}={kv.Value}"))}");
        Console.WriteLine($"  Retained row: {group.RetainedRowIndex}");
        Console.WriteLine($"  Deleted rows: [{string.Join(", ", group.DeletedRowIndices)}]");
    }
}
```

## 🎯 FAÇADE API METÓDY

### Universal Validation Rule API
```csharp
// FLEXIBLE generic approach - nie hardcoded factory methods
Task<Result<bool>> AddValidationRuleAsync<T>(T rule) where T : IValidationRule;

// Príklady použitia:
await facade.AddValidationRuleAsync(new RequiredFieldValidationRule
{
    ColumnName = "Email",
    ErrorMessage = "Email required"
});

await facade.AddValidationRuleAsync(new RangeValidationRule
{
    ColumnName = "Age",
    MinValue = 18,
    MaxValue = 65
});

// Duplicate detection rule
await facade.AddValidationRuleAsync(new DuplicateDetectionValidationRule
{
    RuleName = "EmailDuplicates",
    ComparisonColumns = ["Email"],
    Strategy = DuplicateComparisonStrategy.ExactMatch
});
```

### Správa pravidiel
```csharp
Task<Result<bool>> RemoveValidationRulesAsync(params string[] columnNames);
Task<Result<bool>> RemoveValidationRuleAsync(string ruleName);
Task<Result<bool>> ClearAllValidationRulesAsync();
```

### Hlavná validačná metóda pre COMPLETE dataset
```csharp
/// <summary>
/// PUBLIC API: Validates ALL non-empty rows in complete dataset
/// ENTERPRISE: Full dataset validation including cached/disk storage data
/// COMPLETE: Validates entire dataset, not just visible/filtered rows
/// LINQ OPTIMIZED: Parallel processing s lazy evaluation
/// THREAD SAFE: Atomic operations, immutable commands
///
/// BEHAVIOR LOGIC:
/// 1. Ak sa pri import/paste VŽDY validujú všetky bunky všetkých riadkov
///    → Táto metóda len zistí či sú všetky validné (quick check)
/// 2. Ak sa pri import/paste NEVALIDUJÚ všetky bunky (čo by sa ale malo)
///    → Táto metóda má spraviť validáciu na všetkých bunkách všetkých riadkov celého datasetu
/// </summary>
Task<Result<bool>> AreAllNonEmptyRowsValidAsync(bool onlyFiltered = false);

// Implementation logic:
// if (HasValidationStateForAllRows())
// {
//     return CheckExistingValidationState();
// }
// else
// {
//     return ValidateAllCellsInAllRowsOfCompleteDataset();
// }

// Táto metóda MUSÍ validovať/skontrolovať:
// - Všetky riadky v pamäti
// - Všetky cached riadky
// - Všetky riadky uložené na disku
// - Kompletný dataset, nie len viditeľnú časť
// - S LINQ optimization a thread safety
```

### Smart Validation Strategy
```csharp
ValidationStrategy GetRecommendedValidationStrategy(
    int rowCount,
    int ruleCount,
    TimeSpan? lastValidationTime = null);

bool ShouldUseRealTimeValidation(
    int rowCount,
    int ruleCount,
    TimeSpan? lastValidationTime = null);

Task<Result<ValidationResult>> ValidateWithOptimalStrategyAsync(
    IEnumerable<IReadOnlyDictionary<string, object?>> data,
    cancellationToken cancellationToken = default);
```

## 📊 VALIDALERTS COLUMN (nie validÁlerts)

```csharp
// SPRÁVNE POMENOVANIE: validAlerts (nie validÁlerts)
public sealed record ValidationAlert
{
    public required string ColumnName { get; init; }
    public required string RuleName { get; init; }
    public required string Message { get; init; }
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

// validAlerts column je vždy viditeľný v grid
// Obsahuje zoznam všetkých validation alerts pre daný riadok
// THREAD SAFE: Immutable record s atomic updates
public IReadOnlyList<ValidationAlert> ValidAlerts { get; set; } = Array.Empty<ValidationAlert>();
```

## ⚡ AUTOMATICKÉ REVALIDOVANIE PRE VŠETKY PRAVIDLÁ

```csharp
// 🔄 AUTOMATICKÉ REVALIDOVANIE platí pre VŠETKY validačné pravidlá:

// 1. RequiredFieldValidationRule - pri zmene ColumnName
// 2. RangeValidationRule - pri zmene ColumnName
// 3. RegexValidationRule - pri zmene ColumnName
// 4. CustomFunctionValidationRule - pri zmene DependentColumns
// 5. CrossColumnValidationRule - pri zmene DependentColumns
// 6. ConditionalValidationRule - pri zmene DependentColumns
// 7. AsyncValidationRule - pri zmene DependentColumns
// 8. GroupValidationRule - pri zmene DependentColumns
// 9. DuplicateDetectionValidationRule - pri zmene ComparisonColumns

// Implementácia automatického revalidovania s LINQ optimization:
internal sealed class AutomaticRevalidationService
{
    private readonly ConcurrentDictionary<string, HashSet<IValidationRule>> _columnToRulesMap = new();
    private readonly ObjectPool<ValidationContext> _contextPool;

    public void RegisterRule(IValidationRule rule)
    {
        var dependentColumns = GetDependentColumns(rule);
        foreach (var column in dependentColumns)
        {
            _columnToRulesMap.AddOrUpdate(
                column,
                new HashSet<IValidationRule> { rule },
                (key, existing) => { existing.Add(rule); return existing; });
        }
    }

    // LINQ optimized + thread safe revalidation
    public async Task OnColumnValueChanged(string columnName, object? newValue,
        IReadOnlyDictionary<string, object?> rowData)
    {
        if (_columnToRulesMap.TryGetValue(columnName, out var rules))
        {
            // Parallel LINQ processing s object pooling
            var tasks = rules.AsParallel()
                .Select(async rule => await ValidateRuleAsync(rule, rowData))
                .ToArray();

            await Task.WhenAll(tasks);
        }
    }

    // Special handling pre duplicate detection rules
    public async Task OnDatasetChanged(IEnumerable<IReadOnlyDictionary<string, object?>> allRows)
    {
        var duplicateRules = _columnToRulesMap.Values
            .SelectMany(rules => rules)
            .OfType<DuplicateDetectionValidationRule>()
            .Distinct();

        foreach (var duplicateRule in duplicateRules)
        {
            await ValidateDuplicatesAsync(duplicateRule, allRows);
        }
    }
}
```

## 🧠 SMART REAL-TIME VS BATCH VALIDATION

```csharp
public enum ValidationStrategy
{
    RealTime,        // Okamžite pri zmene
    Batch,           // Periodicky alebo na požiadanie
    Automatic        // Smart rozhodovanie
}

// Smart decision making algoritmus s LINQ optimization:
public ValidationStrategy GetRecommendedValidationStrategy(
    int rowCount,
    int ruleCount,
    TimeSpan? lastValidationTime = null)
{
    // Prahy pre rozhodovanie s performance optimization:
    const int REAL_TIME_ROW_THRESHOLD = 1000;
    const int REAL_TIME_RULE_THRESHOLD = 20;
    const int PERFORMANCE_TIME_THRESHOLD_MS = 500;

    // Real-time ak:
    // - Málo riadkov a pravidiel
    // - Posledná validácia bola rýchla
    // - LINQ parallel processing je efektívny
    if (rowCount <= REAL_TIME_ROW_THRESHOLD &&
        ruleCount <= REAL_TIME_RULE_THRESHOLD &&
        (lastValidationTime?.TotalMilliseconds ?? 0) <= PERFORMANCE_TIME_THRESHOLD_MS)
    {
        return ValidationStrategy.RealTime;
    }

    // Batch pre veľké datasets s streaming optimization
    return ValidationStrategy.Batch;
}
```

## 🔧 VÝSLEDNÉ OBJEKTY

### ValidationResult
```csharp
public sealed record ValidationResult
{
    public required bool IsValid { get; init; }
    public required IReadOnlyList<ValidationAlert> Alerts { get; init; }
    public required ValidationStatistics Statistics { get; init; }
    public TimeSpan ValidationDuration { get; init; }
    public ValidationStrategy UsedStrategy { get; init; }
    public DateTime ValidatedAt { get; init; } = DateTime.UtcNow;
}

public sealed record ValidationStatistics
{
    public int TotalRowsValidated { get; init; }
    public int ValidRows { get; init; }
    public int InvalidRows { get; init; }
    public int RulesExecuted { get; init; }
    public int AsyncRulesCount { get; init; }
    public TimeSpan AverageRuleExecutionTime { get; init; }
    public bool UsedParallelProcessing { get; init; }
    public int ObjectPoolHits { get; init; }
    // Enhanced statistics for duplicates
    public int DuplicateGroupsFound { get; init; }
    public int TotalDuplicateRows { get; init; }
}
```

## 🎯 PERFORMANCE & OPTIMIZATION

### LINQ Optimizations
- **Lazy evaluation** pre veľké datasets
- **Parallel processing** pre batch validations
- **Streaming** pre real-time scenarios
- **Object pooling** pre ValidationContext
- **Minimal allocations** s immutable commands
- **Hash-based duplicate detection** pre performance pri veľkých datasetoch

### Thread Safety
- **Immutable commands** a value objects
- **Atomic RowNumber updates**
- **ConcurrentDictionary** pre rule mappings
- **Thread-safe collections** pre alerts
- **Concurrent duplicate detection** s parallel LINQ

### DI Integration
- **Command factory methods** s dependency injection support
- **Service provider integration** pre external validations
- **Interface contracts preservation** pri refactoringu

## 🎯 KĽÚČOVÉ VYLEPŠENIA PODĽA KOREKCIÍ

1. **🔄 Automatické revalidovanie** - platí pre **VŠETKY** validačné pravidlá, vrátane duplicate detection
2. **📋 Správne pomenovanie** - `validAlerts` column (nie `validÁlerts`)
3. **🔧 Flexibilné pravidlá** - nie hardcoded factory methods, ale flexible object creation
4. **📊 Kompletná validácia** - `AreAllNonEmptyRowsValidAsync` má behavioral logic
5. **⚡ Performance optimization** - LINQ, parallel processing, object pooling, thread safety
6. **🏗️ Clean Architecture** - Commands v Core, processing v Application, hybrid DI support
7. **🔄 Complete replacement** - .oldbackup_timestamp files, žiadna backward compatibility, zachované DI contracts
8. **🗑️ Validation-based deletion** - comprehensive row deletion s duplicate detection capabilities
9. **🔍 Advanced duplicate handling** - multiple comparison strategies, retention policies, performance optimized

---

## **🔍 LOGGING ŠPECIFIKÁCIA PRE VALIDATION OPERÁCIE**

### **Internal DI Registration & Service Distribution**
Všetky validation logging services sú registrované v **`Infrastructure/Services/InternalServiceRegistration.cs`** a distribuované cez internal DI do `ValidationService`:

```csharp
// V InternalServiceRegistration.cs
services.AddSingleton<IValidationLogger<ValidationService>, ValidationLogger<ValidationService>>();
services.AddSingleton<IOperationLogger<ValidationService>, OperationLogger<ValidationService>>();
services.AddSingleton<ICommandLogger<ValidationService>, CommandLogger<ValidationService>>();

// V ValidationService constructor
public ValidationService(
    ILogger<ValidationService> logger,
    IValidationLogger<ValidationService> validationLogger,
    IOperationLogger<ValidationService> operationLogger,
    ICommandLogger<ValidationService> commandLogger)
```

### **Validation Rule Logging Integration**
Validačný systém implementuje comprehensive logging pre všetky typy validačných pravidiel vrátane duplicate detection s automatickým revalidovaním a smart strategy detection.

### **Rule Execution Logging**
```csharp
// Universal validation rule logging
await _validationLogger.LogRuleExecution(rule.RuleName, rule.GetType().Name,
    isValid: result.IsValid, duration: executionTime);

_logger.LogInformation("Validation rule '{RuleName}' executed: valid={IsValid}, column='{ColumnName}', duration={Duration}ms",
    rule.RuleName, result.IsValid, rule.ColumnName, executionTime.TotalMilliseconds);

// Duplicate detection specific logging
if (rule is DuplicateDetectionValidationRule duplicateRule)
{
    _validationLogger.LogDuplicateDetection(duplicateRule.RuleName,
        duplicateGroups.Count, totalDuplicates, executionTime);

    _logger.LogInformation("Duplicate detection '{RuleName}' found {GroupCount} groups with {TotalDuplicates} duplicates in {Duration}ms",
        duplicateRule.RuleName, duplicateGroups.Count, totalDuplicates, executionTime.TotalMilliseconds);
}

// Async validation specific logging
if (rule is AsyncValidationRule asyncRule)
{
    _validationLogger.LogAsyncValidation(asyncRule.RuleName, result.IsValid,
        executionTime, wasTimedOut: executionTime > asyncRule.ValidationTimeout);
}
```

### **Row Deletion Logging**
```csharp
// Validation-based row deletion logging
_logger.LogInformation("Starting validation-based row deletion: mode={Mode}, criteria={Criteria}, rows={RowCount}",
    validationCriteria.Mode, GetCriteriaDescription(validationCriteria), totalRows);

// Duplicate deletion specific logging
if (validationCriteria.DuplicateMode.HasValue)
{
    _logger.LogInformation("Duplicate deletion mode: {Mode}, retention={Retention}, comparison_columns=[{Columns}]",
        validationCriteria.DuplicateMode.Value,
        validationCriteria.RetentionStrategy ?? DuplicateRetentionStrategy.FirstOccurrence,
        string.Join(",", validationCriteria.DuplicateComparisonColumns ?? Array.Empty<string>()));
}

// Deletion results logging
_logger.LogInformation("Validation-based deletion completed: evaluated={Evaluated}, deleted={Deleted}, remaining={Remaining}, duration={Duration}ms",
    result.TotalRowsEvaluated, result.RowsDeleted, result.RemainingRows, result.OperationDuration.TotalMilliseconds);

if (result.DuplicateSummary != null)
{
    _logger.LogInformation("Duplicate deletion summary: groups={Groups}, duplicates_deleted={Deleted}, originals_retained={Retained}",
        result.DuplicateSummary.TotalDuplicateGroups,
        result.DuplicateSummary.DuplicatesDeleted,
        result.DuplicateSummary.OriginalsRetained);
}
```

### **Automatic Revalidation Logging**
```csharp
// Automatic revalidation trigger logging
_validationLogger.LogAutomaticRevalidation(changedColumnName,
    affectedRulesCount, revalidationDuration);

_logger.LogInformation("Automatic revalidation triggered: column='{ColumnName}' affected {RuleCount} rules in {Duration}ms",
    changedColumnName, affectedRulesCount, revalidationDuration.TotalMilliseconds);

// Dataset-wide revalidation for duplicate rules
_logger.LogInformation("Dataset revalidation for duplicate detection: {RuleCount} duplicate rules, {RowCount} rows, duration={Duration}ms",
    duplicateRulesCount, totalRows, revalidationDuration.TotalMilliseconds);

// Bulk revalidation logging
_logger.LogInformation("Bulk revalidation completed: {ValidRows}/{TotalRows} rows valid, {RuleCount} rules, strategy={Strategy}",
    validationResult.Statistics.ValidRows, validationResult.Statistics.TotalRowsValidated,
    validationResult.Statistics.RulesExecuted, usedStrategy);
```

### **Smart Strategy Logging**
```csharp
// Smart validation strategy selection logging
_logger.LogInformation("Smart validation strategy selected: {Strategy} for {RuleCount} rules, {RowCount} rows",
    selectedStrategy, totalRules, totalRows);

// Performance threshold logging
if (estimatedTime > TimeSpan.FromMilliseconds(500))
{
    _logger.LogWarning("Validation performance threshold exceeded: estimated {EstimatedTime}ms, switching to batch mode",
        estimatedTime.TotalMilliseconds);
}

// Strategy switch logging
_logger.LogInformation("Validation strategy switched: {OldStrategy} → {NewStrategy} due to {Reason}",
    oldStrategy, newStrategy, switchReason);
```

### **Logging Levels Usage:**
- **Information**: Successful validations, rule additions, strategy selections, performance metrics
- **Warning**: Validation failures (Warning/Information severity), performance issues, strategy switches
- **Error**: Validation failures (Error severity), rule evaluation errors, configuration issues
- **Critical**: Validation failures (Critical severity), system-level validation failures, data corruption

## 🧮 CORE VALIDATION ALGORITHMS INFRASTRUCTURE

### **ValidationAlgorithms.cs** - Pure Functional Validation Engine

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Utilities;

/// <summary>
/// ENTERPRISE: Pure functional validation algorithms for maximum performance and testability
/// FUNCTIONAL PARADIGM: Stateless algorithms without side effects
/// HYBRID APPROACH: Functional algorithms within OOP service architecture
/// THREAD SAFE: Immutable functions suitable for concurrent execution
/// </summary>
internal static class ValidationAlgorithms
{
    /// <summary>
    /// PURE FUNCTION: Combine validation results with intelligent aggregation logic
    /// BUSINESS LOGIC: Comprehensive error aggregation for enterprise scenarios
    /// PERFORMANCE: Efficient result combination with severity prioritization
    /// </summary>
    public static ValidationResult CombineResults(IReadOnlyList<ValidationResult> results)
    {
        if (results == null) throw new ArgumentNullException(nameof(results));
        if (!results.Any()) return ValidationResult.Success();

        // Filter out null results
        var validResults = results.Where(r => r != null).ToList();
        if (!validResults.Any()) return ValidationResult.Success();

        // If any result is invalid, the combined result is invalid
        var invalidResults = validResults.Where(r => !r.IsValid).ToList();
        if (!invalidResults.Any()) return ValidationResult.Success();

        // Determine overall severity (highest severity wins)
        var highestSeverity = invalidResults.Max(r => r.Severity);

        // Combine error messages
        var errorMessages = invalidResults
            .SelectMany(r => r.ErrorMessages)
            .Distinct()
            .ToList();

        // Combine affected columns
        var affectedColumns = invalidResults
            .SelectMany(r => r.AffectedColumns)
            .Distinct()
            .ToList();

        return ValidationResult.Failure(errorMessages, highestSeverity, affectedColumns);
    }

    /// <summary>
    /// PURE FUNCTION: Determine validation chain termination based on policy
    /// PERFORMANCE: Early termination for expensive validation chains
    /// BUSINESS LOGIC: Configurable stop policies for different scenarios
    /// </summary>
    public static bool ShouldStopEvaluation(ValidationResult result, ValidationStopPolicy policy)
    {
        if (result == null) return false;

        return policy switch
        {
            ValidationStopPolicy.Never => false,
            ValidationStopPolicy.OnFirstError => !result.IsValid,
            ValidationStopPolicy.OnCriticalError => !result.IsValid && result.Severity == ValidationSeverity.Critical,
            ValidationStopPolicy.OnError => !result.IsValid && result.Severity >= ValidationSeverity.Error,
            ValidationStopPolicy.OnWarning => !result.IsValid && result.Severity >= ValidationSeverity.Warning,
            _ => false
        };
    }

    /// <summary>
    /// PURE FUNCTION: Evaluate validation rule with comprehensive error handling
    /// ENTERPRISE: Business rule evaluation with context awareness
    /// TYPE SAFETY: Pattern matching for different rule types
    /// </summary>
    public static ValidationResult EvaluateRule(
        IValidationRule rule,
        object? value,
        IReadOnlyDictionary<string, object?> rowData,
        ValidationContext context)
    {
        if (rule == null) throw new ArgumentNullException(nameof(rule));
        if (rowData == null) throw new ArgumentNullException(nameof(rowData));
        if (context == null) throw new ArgumentNullException(nameof(context));

        try
        {
            // Check if rule should be applied in current context
            if (!ShouldApplyRule(rule, context))
            {
                return ValidationResult.Success();
            }

            // Perform rule-specific validation with pattern matching
            return rule switch
            {
                RequiredRule requiredRule => EvaluateRequiredRule(requiredRule, value),
                RangeRule rangeRule => EvaluateRangeRule(rangeRule, value),
                RegexRule regexRule => EvaluateRegexRule(regexRule, value),
                CustomRule customRule => EvaluateCustomRule(customRule, value, rowData, context),
                _ => rule.Validate(value, rowData, context)
            };
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure(
                new[] { $"Validation rule '{rule.RuleName}' failed with error: {ex.Message}" },
                ValidationSeverity.Error,
                new[] { rule.ColumnName });
        }
    }

    /// <summary>
    /// PURE FUNCTION: Calculate validation confidence score for quality metrics
    /// ENTERPRISE: Quality assurance metrics for validation results
    /// ANALYTICS: Statistical analysis of validation performance
    /// </summary>
    public static double CalculateValidationConfidence(
        IReadOnlyList<ValidationResult> results,
        ValidationConfiguration configuration)
    {
        if (results == null || !results.Any()) return 1.0;
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        var totalRules = results.Count;
        var successfulRules = results.Count(r => r.IsValid);
        var warningRules = results.Count(r => !r.IsValid && r.Severity == ValidationSeverity.Warning);

        // Base confidence from success rate
        var baseConfidence = (double)successfulRules / totalRules;

        // Penalty for warnings (less severe than errors)
        var warningPenalty = (warningRules * 0.1) / totalRules;

        // Bonus for comprehensive validation
        var comprehensivenessBonus = Math.Min(0.1, totalRules / 100.0);

        return Math.Max(0.0, Math.Min(1.0, baseConfidence - warningPenalty + comprehensivenessBonus));
    }

    /// <summary>
    /// PURE FUNCTION: Calculate dynamic validation priority based on context
    /// PERFORMANCE: Smart prioritization for optimal validation order
    /// ENTERPRISE: Context-aware priority adjustment for business scenarios
    /// </summary>
    public static int CalculateValidationPriority(IValidationRule rule, ValidationContext context)
    {
        if (rule == null) return 0;
        if (context == null) return rule.Priority;

        var basePriority = rule.Priority;

        // Adjust priority based on validation trigger
        var contextAdjustment = context.Trigger switch
        {
            ValidationTrigger.OnCellChanged => 10, // Higher priority for immediate feedback
            ValidationTrigger.OnRowChanged => 5,   // Medium priority for row-level changes
            ValidationTrigger.OnDataChanged => 0,  // Normal priority for bulk changes
            ValidationTrigger.OnSave => 20,        // Highest priority for save operations
            _ => 0
        };

        // Adjust priority based on rule severity
        var severityAdjustment = rule.Severity switch
        {
            ValidationSeverity.Critical => 50,
            ValidationSeverity.Error => 30,
            ValidationSeverity.Warning => 10,
            ValidationSeverity.Information => 0,
            _ => 0
        };

        return basePriority + contextAdjustment + severityAdjustment;
    }

    /// <summary>
    /// PURE FUNCTION: Filter and prioritize applicable validation rules
    /// PERFORMANCE: Efficient rule selection for validation scenarios
    /// OPTIMIZATION: Smart rule ordering for optimal execution
    /// </summary>
    public static IReadOnlyList<IValidationRule> FilterApplicableRules(
        IReadOnlyList<IValidationRule> rules,
        ValidationContext context)
    {
        if (rules == null) return Array.Empty<IValidationRule>();
        if (context == null) throw new ArgumentNullException(nameof(context));

        return rules
            .Where(rule => ShouldApplyRule(rule, context))
            .OrderByDescending(rule => CalculateValidationPriority(rule, context))
            .ToList();
    }

    /// <summary>
    /// PURE FUNCTION: Validate data type compatibility with business rules
    /// TYPE SAFETY: Comprehensive type checking for business data
    /// FLEXIBILITY: Support for type conversion and compatibility
    /// </summary>
    public static bool IsDataTypeValid(object? value, Type expectedType, bool allowNull = true)
    {
        if (value == null) return allowNull;
        if (expectedType == null) return true;

        var valueType = value.GetType();

        // Direct type match
        if (expectedType.IsAssignableFrom(valueType))
            return true;

        // Numeric type compatibility
        if (IsNumericType(expectedType) && IsNumericType(valueType))
            return true;

        // String conversion compatibility
        if (expectedType == typeof(string))
            return true;

        // Try conversion
        return CanConvertType(value, expectedType);
    }

    /// <summary>
    /// PURE FUNCTION: Evaluate required field validation with comprehensive empty checks
    /// BUSINESS LOGIC: Enterprise-grade required field validation
    /// </summary>
    private static ValidationResult EvaluateRequiredRule(RequiredRule rule, object? value)
    {
        var isEmpty = value switch
        {
            null => true,
            string str => string.IsNullOrWhiteSpace(str),
            Array array => array.Length == 0,
            System.Collections.ICollection collection => collection.Count == 0,
            _ => false
        };

        return isEmpty
            ? ValidationResult.Failure(
                new[] { rule.ErrorMessage ?? $"Field '{rule.ColumnName}' is required" },
                rule.Severity,
                new[] { rule.ColumnName })
            : ValidationResult.Success();
    }

    /// <summary>
    /// PURE FUNCTION: Evaluate range validation with type-safe comparisons
    /// TYPE SAFETY: Safe range validation with automatic type conversion
    /// </summary>
    private static ValidationResult EvaluateRangeRule(RangeRule rule, object? value)
    {
        if (value == null) return ValidationResult.Success(); // Null values skip range validation

        if (!TryConvertToComparable(value, out var comparableValue))
        {
            return ValidationResult.Failure(
                new[] { $"Value '{value}' cannot be compared for range validation" },
                ValidationSeverity.Error,
                new[] { rule.ColumnName });
        }

        var inRange = true;

        if (rule.MinValue != null && comparableValue.CompareTo(rule.MinValue) < 0)
            inRange = false;

        if (rule.MaxValue != null && comparableValue.CompareTo(rule.MaxValue) > 0)
            inRange = false;

        return inRange
            ? ValidationResult.Success()
            : ValidationResult.Failure(
                new[] { rule.ErrorMessage ?? $"Value must be between {rule.MinValue} and {rule.MaxValue}" },
                rule.Severity,
                new[] { rule.ColumnName });
    }

    /// <summary>
    /// PURE FUNCTION: Evaluate regex pattern validation with performance optimization
    /// PATTERN MATCHING: Regex validation with integrated search filter algorithms
    /// </summary>
    private static ValidationResult EvaluateRegexRule(RegexRule rule, object? value)
    {
        if (value == null) return ValidationResult.Success(); // Null values skip regex validation

        var text = value.ToString() ?? string.Empty;
        var isMatch = SearchFilterAlgorithms.IsRegexMatch(text, rule.Pattern, rule.CaseSensitive);

        return isMatch
            ? ValidationResult.Success()
            : ValidationResult.Failure(
                new[] { rule.ErrorMessage ?? $"Value does not match required pattern" },
                rule.Severity,
                new[] { rule.ColumnName });
    }

    /// <summary>
    /// PURE FUNCTION: Evaluate custom validation rule with error handling
    /// EXTENSIBILITY: Support for custom business validation logic
    /// </summary>
    private static ValidationResult EvaluateCustomRule(
        CustomRule rule,
        object? value,
        IReadOnlyDictionary<string, object?> rowData,
        ValidationContext context)
    {
        try
        {
            return rule.ValidatorFunction(value, rowData, context);
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure(
                new[] { $"Custom validation failed: {ex.Message}" },
                ValidationSeverity.Error,
                new[] { rule.ColumnName });
        }
    }

    /// <summary>
    /// PURE FUNCTION: Determine rule applicability in validation context
    /// CONTEXT AWARENESS: Smart rule filtering based on validation context
    /// </summary>
    private static bool ShouldApplyRule(IValidationRule rule, ValidationContext context)
    {
        // Check if rule is enabled
        if (!rule.IsEnabled) return false;

        // Check trigger compatibility
        if (!rule.ApplicableTriggers.Contains(context.Trigger)) return false;

        // Check severity threshold
        if (context.MinimumSeverity.HasValue && rule.Severity < context.MinimumSeverity.Value)
            return false;

        return true;
    }

    /// <summary>
    /// PURE FUNCTION: Check numeric type compatibility
    /// TYPE CHECKING: Comprehensive numeric type detection
    /// </summary>
    private static bool IsNumericType(Type type)
    {
        return type == typeof(byte) || type == typeof(sbyte) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong) ||
               type == typeof(float) || type == typeof(double) ||
               type == typeof(decimal);
    }

    /// <summary>
    /// PURE FUNCTION: Safe type conversion checking
    /// TYPE SAFETY: Exception-free type conversion testing
    /// </summary>
    private static bool CanConvertType(object value, Type targetType)
    {
        try
        {
            Convert.ChangeType(value, targetType);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// PURE FUNCTION: Convert value to comparable type for range validation
    /// TYPE CONVERSION: Safe conversion with fallback strategies
    /// </summary>
    private static bool TryConvertToComparable(object? value, out IComparable? comparable)
    {
        comparable = null;

        if (value is IComparable comp)
        {
            comparable = comp;
            return true;
        }

        if (value is string str && double.TryParse(str, out var numericValue))
        {
            comparable = numericValue;
            return true;
        }

        return false;
    }
}

/// <summary>
/// ENTERPRISE: Validation stop policies for performance optimization
/// FUNCTIONAL: Immutable enumeration for pure function usage
/// PERFORMANCE: Configurable validation termination strategies
/// </summary>
internal enum ValidationStopPolicy
{
    Never,           // Always validate all rules
    OnFirstError,    // Stop on any validation failure
    OnCriticalError, // Stop only on critical errors
    OnError,         // Stop on errors and critical errors
    OnWarning        // Stop on warnings, errors, and critical errors
}
```

## 🎯 VALIDATION ALGORITHMS INTEGRATION PATTERNS

### **Application Layer Integration**
```csharp
// ValidationService.cs - Integration with pure functional algorithms
internal sealed class ValidationService : IValidationService
{
    public async Task<ValidationResult> ValidateValueAsync(
        string columnName,
        object? value,
        IReadOnlyDictionary<string, object?> rowData,
        cancellationToken cancellationToken = default)
    {
        var applicableRules = _rulesCache.GetRulesForColumn(columnName);
        var context = new ValidationContext
        {
            Trigger = ValidationTrigger.OnCellChanged,
            ServiceProvider = _serviceProvider
        };

        // Filter applicable rules using pure algorithms
        var filteredRules = ValidationAlgorithms.FilterApplicableRules(applicableRules, context);

        var results = new List<ValidationResult>();
        var stopPolicy = _configuration.StopPolicy;

        foreach (var rule in filteredRules)
        {
            // Evaluate rule using pure functional algorithms
            var result = ValidationAlgorithms.EvaluateRule(rule, value, rowData, context);
            results.Add(result);

            // Check for early termination
            if (ValidationAlgorithms.ShouldStopEvaluation(result, stopPolicy))
            {
                break;
            }
        }

        // Combine results using pure algorithms
        var combinedResult = ValidationAlgorithms.CombineResults(results);

        // Calculate confidence score for analytics
        var confidenceScore = ValidationAlgorithms.CalculateValidationConfidence(results, _configuration);

        _logger.LogInformation("Validation completed: column={Column}, valid={IsValid}, confidence={Confidence:P2}",
            columnName, combinedResult.IsValid, confidenceScore);

        return combinedResult;
    }

    public async Task<ValidationResult> ValidateRowAsync(
        IReadOnlyDictionary<string, object?> rowData,
        cancellationToken cancellationToken = default)
    {
        var allRules = _rulesCache.GetAllRules();
        var context = new ValidationContext
        {
            Trigger = ValidationTrigger.OnRowChanged,
            ServiceProvider = _serviceProvider
        };

        // Apply algorithmic rule filtering and prioritization
        var applicableRules = ValidationAlgorithms.FilterApplicableRules(allRules, context);

        var results = new List<ValidationResult>();

        foreach (var rule in applicableRules)
        {
            var value = rowData.TryGetValue(rule.ColumnName, out var cellValue) ? cellValue : null;
            var result = ValidationAlgorithms.EvaluateRule(rule, value, rowData, context);
            results.Add(result);
        }

        return ValidationAlgorithms.CombineResults(results);
    }
}
// Validation strategy decision logging
_validationLogger.LogValidationStrategy(recommendedStrategy.ToString(),
    rowCount, ruleCount, $"Performance threshold: {lastValidationTime?.TotalMilliseconds}ms");

_logger.LogInformation("Validation strategy selected: {Strategy} for {RowCount} rows, {RuleCount} rules (last validation: {LastDuration}ms)",
    recommendedStrategy, rowCount, ruleCount, lastValidationTime?.TotalMilliseconds ?? 0);

// Real-time vs Batch decision reasoning
if (recommendedStrategy == ValidationStrategy.RealTime)
{
    _logger.LogInformation("Real-time validation enabled: dataset size and complexity within thresholds");
}
else
{
    _logger.LogWarning("Batch validation recommended: large dataset or complex rules detected");
}
```

### **ValidationAlerts Column Logging**
```csharp
// ValidationAlerts updates logging
_logger.LogInformation("ValidationAlerts updated for row {RowIndex}: {AlertCount} alerts, severities=[{Severities}]",
    rowIndex, validationAlerts.Count, string.Join(",", validationAlerts.Select(a => a.Severity)));

// Alert aggregation logging
_logger.LogInformation("Validation summary: {TotalAlerts} alerts across {AffectedRows} rows, {ErrorCount} errors, {WarningCount} warnings, {DuplicateCount} duplicates",
    totalAlerts, affectedRows, errorCount, warningCount, duplicateCount);
```

### **Logging Levels Usage:**
- **Information**: Rule executions, strategy decisions, successful validations, revalidation triggers, duplicate detection results
- **Warning**: Validation timeouts, performance degradation, rule conflicts, duplicate retention decisions
- **Error**: Rule execution failures, validation service errors, configuration errors, deletion operation failures
- **Critical**: Validation system failures, data integrity violations, duplicate handling errors

> Note: Import and Paste flows call `AreAllNonEmptyRowsValidAsync(false)` when `=true`. Export calls the same method with `onlyFiltered` according to the ExportDataCommand semantics.



### AreAllNonEmptyRowsValidAsync (canonical validation entry)

Signature:
```csharp
Task<Result<bool>> AreAllNonEmptyRowsValidAsync(bool onlyFiltered = false);
```

Implementation logic (required behavior):

```text
// Pseudocode
if (NoValidationRulesConfigured())
{
    // Fast-path: nothing to check
    return Result.Success(true);
}

if (HasValidationStateForRequestedScope(onlyFiltered))
{
    // Return cached boolean result for the requested scope (whole dataset or filtered subset)
    return Result.Success(CheckExistingValidationState(onlyFiltered));
}
else
{
    // Perform validation across the requested scope:
    // - Validate all rows in memory
    // - Validate all cached rows (paged out but cached)
    // - Validate all rows on disk
    // - Validate the complete dataset (or the filtered subset when onlyFiltered = true)
    // Implementation should consider batching and LINQ optimizations and must be thread-safe.
    var final = ValidateAllCellsInAllRowsOfRequestedScope(onlyFiltered);
    return Result.Success(final);
}
```

Notes:
- The method MUST validate the **complete dataset** bounds described above, not just the visible UI portion.
- If `onlyFiltered = true` and **no filter is active**, treat as `onlyFiltered = false` (i.e., whole dataset) — same semantics used by Export.
- Validation MUST be thread-safe and should avoid excessive memory spikes by using batching/streaming and by leveraging cached validation state when possible.
- This method is the single canonical entry for both import/paste post-processing and export pre-checking. Import and Export flows call it automatically as described in the Import/Export documentation.
