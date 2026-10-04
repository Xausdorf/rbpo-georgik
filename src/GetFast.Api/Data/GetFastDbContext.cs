using Microsoft.EntityFrameworkCore;

namespace GetFast.Api.Data;

public class GetFastDbContext(DbContextOptions<GetFastDbContext> options) : DbContext(options);
