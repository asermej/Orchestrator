# Security & Performance Standards

**CRITICAL**: Security and performance are not optional - they are architectural requirements.

## Security Requirements

### 1. **Input Validation & Sanitization**

#### **API Layer Validation**
```csharp
// ✅ DO: Validate at API boundary
[HttpPost]
public async Task<ActionResult<CustomerResource>> CreateAsync([FromBody] CreateCustomerResource resource)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }
    
    // Additional validation
    if (string.IsNullOrWhiteSpace(resource.Email))
    {
        return BadRequest(new ErrorResponse { Message = "Email is required" });
    }
}
```

#### **Domain Layer Business Validation**
```csharp
// ✅ DO: Validate business rules in domain
public async Task<Guid> CreateCustomer(Customer customer)
{
    // Business validation
    if (!IsValidEmail(customer.Email))
        throw new CustomerValidationException("Invalid email format");
        
    if (await EmailAlreadyExists(customer.Email))
        throw new CustomerDuplicateException("Email already exists");
        
    return await _dataFacade.CreateCustomer(customer);
}
```

### 2. **SQL Injection Prevention**

#### **ALWAYS Use Parameterized Queries**
```csharp
// ✅ DO: Parameterized queries
public async Task<Customer?> GetCustomerByEmail(string email)
{
    const string sql = "SELECT * FROM customers WHERE email = @Email";
    using var connection = new NpgsqlConnection(_connectionString);
    return await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { Email = email });
}

// ❌ DON'T: String concatenation
public async Task<Customer?> GetCustomerByEmail(string email)
{
    var sql = $"SELECT * FROM customers WHERE email = '{email}'"; // VULNERABLE!
    // ... rest of code
}
```

#### **Dynamic Query Building**
```csharp
// ✅ DO: Safe dynamic queries
public async Task<IEnumerable<Customer>> SearchCustomers(CustomerSearchCriteria criteria)
{
    var sqlBuilder = new StringBuilder("SELECT * FROM customers WHERE 1=1");
    var parameters = new DynamicParameters();
    
    if (!string.IsNullOrEmpty(criteria.FirstName))
    {
        sqlBuilder.Append(" AND first_name ILIKE @FirstName");
        parameters.Add("FirstName", $"%{criteria.FirstName}%");
    }
    
    if (!string.IsNullOrEmpty(criteria.Email))
    {
        sqlBuilder.Append(" AND email = @Email");
        parameters.Add("Email", criteria.Email);
    }
    
    using var connection = new NpgsqlConnection(_connectionString);
    return await connection.QueryAsync<Customer>(sqlBuilder.ToString(), parameters);
}
```

### 3. **Authentication & Authorization**

#### **Controller Security**
```csharp
// ✅ DO: Secure endpoints
[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // Require authentication
public class CustomerController : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,Manager")] // Role-based authorization
    public async Task<ActionResult<PaginatedResult<CustomerResource>>> SearchAsync(
        [FromQuery] SearchCustomerCriteria criteria)
    {
        // Implementation
    }
    
    [HttpPost]
    [Authorize(Policy = "CanCreateCustomers")] // Policy-based authorization
    public async Task<ActionResult<CustomerResource>> CreateAsync(
        [FromBody] CreateCustomerResource resource)
    {
        // Implementation
    }
}
```

### 4. **Data Protection**

#### **Sensitive Data Handling**
```csharp
// ✅ DO: Protect sensitive data
public class Customer : Entity
{
    public string Email { get; set; } = string.Empty;
    
    [JsonIgnore] // Don't serialize sensitive data
    public string PasswordHash { get; set; } = string.Empty;
    
    [PersonalData] // Mark for GDPR compliance
    public string PhoneNumber { get; set; } = string.Empty;
}
```

#### **Logging Security**
```csharp
// ✅ DO: Safe logging
_logger.LogInformation("Customer created with ID: {CustomerId}", customerId);

// ❌ DON'T: Log sensitive data
_logger.LogInformation("Customer created: {@Customer}", customer); // May contain sensitive data
```

## Performance Requirements

### 1. **Async/Await Pattern**

#### **ALWAYS Use Async for I/O Operations**
```csharp
// ✅ DO: Async all the way
public async Task<Customer?> GetCustomerById(Guid id)
{
    const string sql = "SELECT * FROM customers WHERE id = @Id";
    using var connection = new NpgsqlConnection(_connectionString);
    return await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { Id = id });
}

// ❌ DON'T: Blocking calls
public Customer? GetCustomerById(Guid id)
{
    const string sql = "SELECT * FROM customers WHERE id = @Id";
    using var connection = new NpgsqlConnection(_connectionString);
    return connection.QuerySingleOrDefault<Customer>(sql, new { Id = id }); // Blocking!
}
```

#### **ConfigureAwait(false) for Library Code**
```csharp
// ✅ DO: Use ConfigureAwait(false) in domain layer
public async Task<Guid> CreateCustomer(Customer customer)
{
    ValidateCustomer(customer);
    return await _dataFacade.CreateCustomer(customer).ConfigureAwait(false);
}
```

### 2. **Database Performance**

#### **Pagination is MANDATORY**
```csharp
// ✅ DO: Always paginate list operations
public async Task<PaginatedResult<Customer>> SearchCustomers(
    CustomerSearchCriteria criteria, 
    int pageNumber = 1, 
    int pageSize = 10)
{
    const int maxPageSize = 100;
    pageSize = Math.Min(pageSize, maxPageSize);
    
    var offset = (pageNumber - 1) * pageSize;
    
    const string sql = @"
        SELECT * FROM customers 
        WHERE (@SearchTerm IS NULL OR first_name ILIKE @SearchTerm OR last_name ILIKE @SearchTerm)
        ORDER BY created_at DESC
        LIMIT @PageSize OFFSET @Offset";
        
    const string countSql = @"
        SELECT COUNT(*) FROM customers 
        WHERE (@SearchTerm IS NULL OR first_name ILIKE @SearchTerm OR last_name ILIKE @SearchTerm)";
    
    var parameters = new { 
        SearchTerm = string.IsNullOrEmpty(criteria.SearchTerm) ? null : $"%{criteria.SearchTerm}%",
        PageSize = pageSize,
        Offset = offset
    };
    
    using var connection = new NpgsqlConnection(_connectionString);
    var items = await connection.QueryAsync<Customer>(sql, parameters);
    var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);
    
    return new PaginatedResult<Customer>
    {
        Items = items,
        TotalCount = totalCount,
        PageNumber = pageNumber,
        PageSize = pageSize
    };
}
```

#### **Index Usage Guidelines**
```sql
-- ✅ DO: Create indexes for common queries
CREATE INDEX idx_customers_email ON customers(email);
CREATE INDEX idx_customers_created_at ON customers(created_at);
CREATE INDEX idx_customers_search ON customers(first_name, last_name);
CREATE INDEX idx_customers_status_created ON customers(status, created_at);

-- ✅ DO: Partial indexes for common filters
CREATE INDEX idx_customers_active ON customers(created_at) WHERE status = 'Active';
```

### 3. **Memory Management**

#### **Dispose Pattern**
```csharp
// ✅ DO: Proper resource disposal
public class CustomerManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private readonly DataFacade _dataFacade;
    private bool _disposed = false;

    public CustomerManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
        var configurationProvider = serviceLocator.CreateConfigurationProvider();
        _dataFacade = new DataFacade(configurationProvider.GetDbConnectionString());
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _dataFacade?.Dispose();
            _disposed = true;
        }
    }
}
```

#### **Connection Management**
```csharp
// ✅ DO: Use using statements for connections
public async Task<Customer?> GetCustomerById(Guid id)
{
    const string sql = "SELECT * FROM customers WHERE id = @Id";
    using var connection = new NpgsqlConnection(_connectionString);
    return await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { Id = id });
}

// ❌ DON'T: Leave connections open
public async Task<Customer?> GetCustomerById(Guid id)
{
    const string sql = "SELECT * FROM customers WHERE id = @Id";
    var connection = new NpgsqlConnection(_connectionString); // Not disposed!
    return await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { Id = id });
}
```

## API Design Standards

### 1. **HTTP Status Codes**
```csharp
// ✅ DO: Use appropriate status codes
[HttpPost]
public async Task<ActionResult<CustomerResource>> CreateAsync([FromBody] CreateCustomerResource resource)
{
    try
    {
        var customer = CustomerMapper.ToDomain(resource);
        var customerId = await _domainFacade.CreateCustomer(customer);
        var created = await _domainFacade.GetCustomerById(customerId);
        
        return CreatedAtAction(nameof(GetById), new { id = customerId }, CustomerMapper.ToResource(created));
    }
    catch (CustomerValidationException ex)
    {
        return BadRequest(new ErrorResponse { Message = ex.Message });
    }
    catch (CustomerDuplicateException ex)
    {
        return Conflict(new ErrorResponse { Message = ex.Message });
    }
}

[HttpGet("{id}")]
public async Task<ActionResult<CustomerResource>> GetById(Guid id)
{
    var customer = await _domainFacade.GetCustomerById(id);
    if (customer == null)
    {
        return NotFound(new ErrorResponse { Message = "Customer not found" });
    }
    
    return Ok(CustomerMapper.ToResource(customer));
}
```

### 2. **Rate Limiting**
```csharp
// ✅ DO: Implement rate limiting
[HttpPost]
[EnableRateLimiting("CreateCustomerPolicy")]
public async Task<ActionResult<CustomerResource>> CreateAsync([FromBody] CreateCustomerResource resource)
{
    // Implementation
}
```

### 3. **CORS Configuration**
```csharp
// ✅ DO: Configure CORS properly
services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        builder =>
        {
            builder.WithOrigins("https://yourdomain.com")
                   .AllowedHeaders("Content-Type", "Authorization")
                   .AllowedMethods("GET", "POST", "PUT", "DELETE")
                   .AllowCredentials();
        });
});
```

## Security Anti-Patterns

### **Never Do These**

#### **SQL Injection Vulnerabilities**
```csharp
// ❌ NEVER: String concatenation in SQL
var sql = $"SELECT * FROM customers WHERE email = '{email}'";

// ❌ NEVER: Dynamic table/column names without validation
var sql = $"SELECT * FROM {tableName} WHERE {columnName} = @Value";
```

#### **Information Disclosure**
```csharp
// ❌ NEVER: Expose internal exceptions
catch (Exception ex)
{
    return BadRequest(ex.ToString()); // Exposes stack trace!
}

// ❌ NEVER: Log sensitive data
_logger.LogInformation("Login attempt: {Username} {Password}", username, password);
```

#### **Authentication Bypass**
```csharp
// ❌ NEVER: Skip authentication for "convenience"
[AllowAnonymous] // Only use when truly needed
public async Task<ActionResult> GetSensitiveData()
{
    // This should require authentication!
}
```

## Performance Anti-Patterns

### **Never Do These**

#### **N+1 Query Problem**
```csharp
// ❌ NEVER: N+1 queries
public async Task<IEnumerable<CustomerWithOrdersResource>> GetCustomersWithOrders()
{
    var customers = await _dataFacade.GetAllCustomers();
    var result = new List<CustomerWithOrdersResource>();
    
    foreach (var customer in customers) // N+1 problem!
    {
        var orders = await _dataFacade.GetOrdersByCustomerId(customer.Id);
        result.Add(new CustomerWithOrdersResource 
        { 
            Customer = customer, 
            Orders = orders 
        });
    }
    
    return result;
}
```

#### **Synchronous I/O**
```csharp
// ❌ NEVER: Blocking calls
public Customer GetCustomer(Guid id)
{
    return _dataFacade.GetCustomerById(id).Result; // Deadlock risk!
}
```

---

**Remember**: Security and performance are not features to be added later - they are architectural foundations that must be built in from the start.

