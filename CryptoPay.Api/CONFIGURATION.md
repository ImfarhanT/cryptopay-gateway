# Configuration Guide

## Environment Variables

For production deployments, use environment variables instead of hardcoding values in `appsettings.json`.

### Connection String
```bash
ConnectionStrings__DefaultConnection="Server=your-server;Database=CryptoPayDb;User Id=user;Password=pass;TrustServerCertificate=True;"
```

### Blockchain Configuration
```bash
Blockchain__EvmRpcUrl="https://mainnet.infura.io/v3/YOUR_INFURA_KEY"
Blockchain__TronGridUrl="https://api.trongrid.io"
```

### Exchange Rates (Optional)
```bash
ExchangeRates__USD__USDT=1.0
ExchangeRates__EUR__USDT=0.92
```

## Generating Secure Keys

### API Key
```bash
openssl rand -hex 32
```

### Webhook Secret
```bash
openssl rand -hex 32
```

## Database Connection Strings

### SQL Server (Windows Authentication)
```
Server=localhost;Database=CryptoPayDb;Trusted_Connection=True;TrustServerCertificate=True;
```

### SQL Server (SQL Authentication)
```
Server=localhost;Database=CryptoPayDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
```

### SQL Server (Azure)
```
Server=tcp:yourserver.database.windows.net,1433;Database=CryptoPayDb;User Id=youruser;Password=yourpass;Encrypt=True;TrustServerCertificate=False;
```

## Production Checklist

- [ ] Use environment variables for all sensitive data
- [ ] Enable HTTPS
- [ ] Restrict CORS to specific origins
- [ ] Secure admin endpoints with authentication
- [ ] Set up proper logging (Serilog, Application Insights, etc.)
- [ ] Configure health checks
- [ ] Set up database backups
- [ ] Implement rate limiting
- [ ] Use connection pooling
- [ ] Configure retry policies for external APIs
