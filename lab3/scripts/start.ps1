Write-Host "Starting Docker Compose services..." -ForegroundColor Green
docker-compose up -d

if ($LASTEXITCODE -eq 0) {
    Write-Host "Services started successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Available services:" -ForegroundColor Yellow
    Write-Host "- Valuator 1: http://localhost:5001"
    Write-Host "- Valuator 2: http://localhost:5002"
    Write-Host "- Valuator 3: http://localhost:5003"
    Write-Host "- Valuator 4: http://localhost:5004"
    Write-Host "- Nginx LB: http://localhost:8080"
    Write-Host "- Redis: localhost:6379"
    
    # Показать статус контейнеров
    Write-Host ""
    Write-Host "Container status:" -ForegroundColor Yellow
    docker-compose ps
} else {
    Write-Host "Failed to start services." -ForegroundColor Red
    Read-Host "Press Enter to continue"
}