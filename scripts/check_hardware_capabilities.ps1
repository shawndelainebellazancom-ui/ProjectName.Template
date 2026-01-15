<#
.SYNOPSIS
    PMCR-O Hardware & Environment Diagnostic Tool
.DESCRIPTION
    Analyzes the host machine to determine AI Training capabilities (LoRA vs Context Injection).
    Checks CPU, RAM, GPU VRAM, Storage, and Software prerequisites.
.NOTES
    Author: PMCR-O Meta-Orchestrator
    Version: 1.0
#>

Clear-Host
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "      PMCR-O HARDWARE DIAGNOSTICS         " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

$capabilities = @{
    "GPU_Type" = "None"
    "VRAM_GB" = 0
    "RAM_GB" = 0
    "Can_Train_LoRA" = $false
    "Recommendation" = "Unknown"
}

# 1. CPU CHECK
# ---------------------------------------------------------
$cpu = Get-CimInstance Win32_Processor
Write-Host "`n[1] CPU" -ForegroundColor Yellow
Write-Host "    Name:   " $cpu.Name
Write-Host "    Cores:  " $cpu.NumberOfCores "Physical /" $cpu.NumberOfLogicalProcessors "Logical"

# 2. RAM CHECK
# ---------------------------------------------------------
$ramObj = Get-CimInstance Win32_ComputerSystem
$ramGB = [Math]::Round($ramObj.TotalPhysicalMemory / 1GB, 2)
$capabilities["RAM_GB"] = $ramGB
Write-Host "`n[2] RAM" -ForegroundColor Yellow
Write-Host "    Total:  " $ramGB "GB"

# 3. GPU CHECK (The Critical Component)
# ---------------------------------------------------------
Write-Host "`n[3] GPU (AI Accelerator)" -ForegroundColor Yellow
try {
    # Try to ask NVIDIA drivers directly (Gold Standard)
    $nvidia = nvidia-smi --query-gpu=name,memory.total,driver_version --format=csv,noheader 2>$null
    if ($nvidia) {
        $gpuDetails = $nvidia -split ","
        $vramExact = $gpuDetails[1].Trim().Split(" ")[0] # Extract number from "24576 MiB"
        $vramGB = [Math]::Round([int]$vramExact / 1024, 2)
        
        $capabilities["GPU_Type"] = "NVIDIA"
        $capabilities["VRAM_GB"] = $vramGB

        Write-Host "    Status:  " -NoNewline
        Write-Host "NVIDIA DETECTED (Compatible)" -ForegroundColor Green
        Write-Host "    Card:    " $gpuDetails[0]
        Write-Host "    VRAM:    " $vramGB "GB"
        Write-Host "    Driver:  " $gpuDetails[2]
    } else {
        throw "NVIDIA-SMI not found"
    }
} catch {
    # Fallback to Windows generic info
    $gpus = Get-CimInstance Win32_VideoController
    foreach ($gpu in $gpus) {
        $vram = [Math]::Round($gpu.AdapterRAM / 1GB, 2)
        Write-Host "    Card:    " $gpu.Name
        Write-Host "    VRAM:    ~" $vram "GB (System Reported)"
        if ($gpu.Name -match "NVIDIA") {
            Write-Host "    Note:    NVIDIA card detected but drivers/nvidia-smi not accessible." -ForegroundColor Red
        }
    }
}

# 4. STORAGE CHECK
# ---------------------------------------------------------
Write-Host "`n[4] Storage (Artifacts & Models)" -ForegroundColor Yellow
$drives = Get-CimInstance Win32_LogicalDisk -Filter "DriveType=3"
foreach ($disk in $drives) {
    $free = [Math]::Round($disk.FreeSpace / 1GB, 2)
    $total = [Math]::Round($disk.Size / 1GB, 2)
    Write-Host "    Drive $($disk.DeviceID) - Free: $free GB / Total: $total GB"
}

# 5. SOFTWARE STACK CHECK
# ---------------------------------------------------------
Write-Host "`n[5] Software Stack" -ForegroundColor Yellow

# Docker
$docker = docker --version 2>$null
if ($docker) { Write-Host "    Docker:  " -NoNewline; Write-Host "OK ($docker)" -ForegroundColor Green } 
else { Write-Host "    Docker:  " -NoNewline; Write-Host "MISSING" -ForegroundColor Red }

# WSL
$wsl = wsl --status 2>$null
if ($wsl) { Write-Host "    WSL2:    " -NoNewline; Write-Host "OK" -ForegroundColor Green }
else { Write-Host "    WSL2:    " -NoNewline; Write-Host "MISSING" -ForegroundColor Red }

# Ollama
$ollama = ollama --version 2>$null
if ($ollama) { Write-Host "    Ollama:  " -NoNewline; Write-Host "OK ($ollama)" -ForegroundColor Green }
else { Write-Host "    Ollama:  " -NoNewline; Write-Host "MISSING" -ForegroundColor Red }

# Python
$python = python --version 2>$null
if ($python) { Write-Host "    Python:  " -NoNewline; Write-Host "OK ($python)" -ForegroundColor Green }
else { Write-Host "    Python:  " -NoNewline; Write-Host "MISSING" -ForegroundColor Red }

# 6. PMCR-O ASSESSMENT
# ---------------------------------------------------------
Write-Host "`n[6] PMCR-O Capability Assessment" -ForegroundColor Yellow

if ($capabilities["GPU_Type"] -eq "NVIDIA") {
    if ($capabilities["VRAM_GB"] -ge 20) {
        $capabilities["Can_Train_LoRA"] = $true
        $capabilities["Recommendation"] = "FULL AUTONOMY (True LoRA)"
        Write-Host "    Tier:    " -NoNewline; Write-Host "GOD TIER" -ForegroundColor Magenta
        Write-Host "    Result:  Hardware supports local fine-tuning (LoRA)."
    }
    elseif ($capabilities["VRAM_GB"] -ge 8) {
        $capabilities["Can_Train_LoRA"] = $false
        $capabilities["Recommendation"] = "HYBRID (Context Injection + Quantized Models)"
        Write-Host "    Tier:    " -NoNewline; Write-Host "DEVELOPER TIER" -ForegroundColor Cyan
        Write-Host "    Result:  Good for inference/running agents. Training will be slow/impossible locally."
    }
    else {
        $capabilities["Recommendation"] = "INFERENCE ONLY"
        Write-Host "    Tier:    " -NoNewline; Write-Host "CONSUMER TIER" -ForegroundColor White
        Write-Host "    Result:  Use quantized small models (Phi-3, Qwen-1.5)."
    }
} else {
    Write-Host "    Tier:    " -NoNewline; Write-Host "CPU MODE" -ForegroundColor Gray
    Write-Host "    Result:  Performance will be limited. High latency expected."
}

Write-Host "`n==========================================" -ForegroundColor Cyan