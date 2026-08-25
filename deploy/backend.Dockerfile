# Build context is the repo root (see docker-compose.yml), so paths below are
# relative to the repo root, not this deploy/ folder.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the API project's csproj first, so restore is cached independently
# of source changes. We deliberately do NOT use a wildcard like
# Backend/**/*.csproj here - that would also match
# ClinicAppointmentBookingSystem.IntegrationTests.csproj and pull the test
# project (and its test-only NuGet packages) into the production image.
COPY Backend/ClinicAppointmentBookingSystem/*.csproj Backend/ClinicAppointmentBookingSystem/
RUN dotnet restore Backend/ClinicAppointmentBookingSystem/ClinicAppointmentBookingSystem.csproj

# Now copy the API project's source only - again, the test project's folder
# is simply never copied into the build context here.
COPY Backend/ClinicAppointmentBookingSystem/ Backend/ClinicAppointmentBookingSystem/
RUN dotnet publish Backend/ClinicAppointmentBookingSystem/ClinicAppointmentBookingSystem.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# APP_UID is provided by the .NET 8+ base images - runs as non-root.
USER $APP_UID

ENTRYPOINT ["dotnet", "ClinicAppointmentBookingSystem.dll"]
