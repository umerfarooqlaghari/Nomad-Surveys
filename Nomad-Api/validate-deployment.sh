#!/bin/bash

# Validation script for Render deployment
echo "🔍 Validating Nomad API deployment files..."

# Check if we're in the right directory
if [ ! -f "Nomad.Api.sln" ]; then
    echo "❌ Error: Nomad.Api.sln not found. Please run this script from the Nomad-Surveys/Nomad-Api directory."
    exit 1
fi

echo "✅ Solution file found"

# Check Dockerfile
if [ ! -f "Dockerfile" ]; then
    echo "❌ Error: Dockerfile not found in root directory"
    exit 1
fi

echo "✅ Dockerfile found"

# Check .dockerignore
if [ ! -f ".dockerignore" ]; then
    echo "❌ Error: .dockerignore not found in root directory"
    exit 1
fi

echo "✅ .dockerignore found"

# Check main project file
if [ ! -f "Nomad.Api/Nomad.Api.csproj" ]; then
    echo "❌ Error: Nomad.Api/Nomad.Api.csproj not found"
    exit 1
fi

echo "✅ Main project file found"

# Check appsettings files
if [ ! -f "Nomad.Api/appsettings.json" ]; then
    echo "❌ Error: appsettings.json not found"
    exit 1
fi

echo "✅ appsettings.json found"

if [ ! -f "Nomad.Api/appsettings.Production.json" ]; then
    echo "❌ Error: appsettings.Production.json not found"
    exit 1
fi

echo "✅ appsettings.Production.json found"

# Check Program.cs
if [ ! -f "Nomad.Api/Program.cs" ]; then
    echo "❌ Error: Program.cs not found"
    exit 1
fi

echo "✅ Program.cs found"

echo ""
echo "🎉 All deployment files are in place!"
echo ""
echo "📋 Deployment checklist:"
echo "   1. Push these files to your GitHub repository"
echo "   2. Create a new Web Service on Render"
echo "   3. Set Root Directory to: Nomad-Surveys/Nomad-Api"
echo "   4. Set Runtime to: Docker"
echo "   5. Configure environment variables"
echo "   6. Deploy!"
echo ""
echo "📖 See RENDER-DEPLOYMENT-GUIDE.md for detailed instructions"
