# See https://aka.ms/customizecontainer to learn how to customize your debug container.
# This Dockerfile is suitable for both local debugging and production builds.

############################
# Runtime image (Alpine)
############################
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base

# Workdir where the app will run
WORKDIR /app

# Install only the runtime dependencies you need
# ffmpeg  - for media processing
# libgdiplus - for System.Drawing on Linux
RUN apk add --no-cache \
    ffmpeg \
    libgdiplus

# Switch to non-root user for security
USER app

# Expose the ports you use in your app
EXPOSE 8080
EXPOSE 8081


############################
# Build image (Alpine SDK)
############################
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

ARG BUILD_CONFIGURATION=Release
ARG VERSION=1.0.0
WORKDIR /src

# Copy project file(s) and restore as a separate layer for better caching
COPY ["source/TranslatorApp/TranslatorApp.csproj", "source/TranslatorApp/"]
COPY ["source/Parsers/Parsers.csproj", "source/Parsers/"]
RUN dotnet restore "./source/TranslatorApp/TranslatorApp.csproj"

# Copy the rest of the source and build
COPY . .
WORKDIR "/src/source/TranslatorApp"
RUN dotnet build "./TranslatorApp.csproj" -c $BUILD_CONFIGURATION -o /app/build /p:Version=$VERSION /p:AssemblyVersion=$VERSION /p:FileVersion=$VERSION


############################
# Publish image
############################
FROM build AS publish

ARG BUILD_CONFIGURATION=Release
ARG VERSION=1.0.0
RUN dotnet publish "./TranslatorApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:Version=$VERSION /p:AssemblyVersion=$VERSION /p:FileVersion=$VERSION


############################
# Final image
############################
FROM base AS final

WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "TranslatorApp.dll"]
