# Render Deployment Guide for Nomad Surveys API

## Prerequisites
- GitHub repository with your code
- Render account (https://render.com)
- PostgreSQL database (can be created on Render)

## Step 1: Create PostgreSQL Database on Render

1. Go to Render Dashboard
2. Click "New" → "PostgreSQL"
3. Configure:
   - **Name**: `nomad-surveys-db`
   - **Database**: `nomad_surveys`
   - **User**: `nomad_surveys_user`
   - **Region**: Choose closest to your users
   - **Plan**: Free tier for testing, paid for production

4. After creation, note down the connection details:
   - **Internal Database URL**: Use this for your app
   - **External Database URL**: For external connections

## Step 2: Deploy the API on Render

1. Go to Render Dashboard
2. Click "New" → "Web Service"
3. Connect your GitHub repository
4. Configure the service:

### Basic Settings
- **Name**: `nomad-surveys-api`
- **Region**: Same as your database
- **Branch**: `main` (or your deployment branch)
- **Root Directory**: `Nomad-Surveys/Nomad-Api`
- **Runtime**: `Docker`

### Build Settings
- **Build Command**: (Leave empty - Docker handles this)
- **Start Command**: (Leave empty - Docker handles this)

### Environment Variables
Add these environment variables in Render:

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
ConnectionStrings__DefaultConnection=<YOUR_POSTGRES_CONNECTION_STRING>
Jwt__Key=<YOUR_JWT_SECRET_KEY>
Jwt__Issuer=NomadSurveys
Jwt__Audience=NomadSurveysUsers
Jwt__ExpiryInHours=24
```

### Important Notes:
- Replace `<YOUR_POSTGRES_CONNECTION_STRING>` with your actual database URL from Step 1
- Replace `<YOUR_JWT_SECRET_KEY>` with a secure random string (minimum 32 characters)
- Use the **Internal Database URL** for better performance and security

### Example Connection String Format:
```
Host=dpg-xxxxx-a.oregon-postgres.render.com;Database=nomad_surveys;Username=nomad_surveys_user;Password=xxxxx;Port=5432;SSL Mode=Require;Trust Server Certificate=true;
```

## Step 3: Configure CORS for Frontend

Update the environment variables to include your Vercel frontend URL:

```
CORS__AllowedOrigins__0=https://your-app-name.vercel.app
CORS__AllowedOrigins__1=https://nomad-surveys.vercel.app
```

## Step 4: Database Migrations

The application will automatically run migrations on startup. If you need to run them manually:

1. Use Render's shell access
2. Run: `dotnet ef database update`

## Step 5: Health Check

After deployment, test your API:
- Health endpoint: `https://your-app-name.onrender.com/api/health`
- Swagger UI: `https://your-app-name.onrender.com/swagger`

## Troubleshooting

### Common Issues:

1. **Database Connection Errors**
   - Verify connection string format
   - Ensure SSL Mode=Require for Render PostgreSQL
   - Check if database is in the same region

2. **JWT Token Issues**
   - Ensure JWT key is at least 32 characters
   - Verify all JWT settings are configured

3. **CORS Issues**
   - Add your Vercel domain to CORS settings
   - Include both www and non-www versions if needed

4. **Build Failures**
   - Check Dockerfile syntax
   - Verify all dependencies are restored
   - Check build logs in Render dashboard

### Monitoring
- Use Render's built-in logging
- Monitor performance metrics
- Set up alerts for downtime

## Security Recommendations

1. **Environment Variables**: Never commit secrets to Git
2. **Database**: Use internal connection strings
3. **JWT**: Use strong, unique keys for production
4. **HTTPS**: Render provides SSL certificates automatically
5. **CORS**: Only allow your frontend domains

## Scaling

For production workloads:
- Upgrade to paid Render plans
- Consider using Render's autoscaling features
- Monitor database performance and upgrade if needed
- Implement caching strategies (Redis)

## Backup Strategy

1. **Database**: Render provides automated backups for paid plans
2. **Code**: Ensure your Git repository is backed up
3. **Environment Variables**: Document all required variables securely
