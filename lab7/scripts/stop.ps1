Write-Host "Stopping Docker Compose services..." -ForegroundColor Yellow
docker-compose down

if ($LASTEXITCODE -eq 0) {
    Write-Host "Services stopped successfully!" -ForegroundColor Green
} else {
    Write-Host "Failed to stop services." -ForegroundColor Red
    Read-Host "Press Enter to continue"
}