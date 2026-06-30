![VisualGGPK3](https://img.shields.io/badge/VisualGGPK3-2.9.30-blue)
![LibGGPK3 upstream](https://img.shields.io/badge/upstream-LibGGPK3%202.7.5-lightgrey)
[![Releases](https://img.shields.io/github/v/release/Baegovda/GGPK_Custom?label=release)](https://github.com/Baegovda/GGPK_Custom/releases)

**[변경 이력 (CHANGELOG)](CHANGELOG.md)** · **[Releases](https://github.com/Baegovda/GGPK_Custom/releases)**

## GGPK_Custom

**Baegovda**의 커스텀 포크입니다. [LibGGPK3](https://github.com/aianlinb/LibGGPK3) 2.7.5를 기반으로 **VisualGGPK3** GUI를 확장·안정화한 저장소입니다.

Path of Exile `Content.ggpk` 및 번들 파일을 열고, 필터·미리보기·즐겨찾기 등으로 탐색할 수 있습니다.

## Quick start (Windows)

```powershell
.\setup.ps1          # .NET 8 SDK 등 (최초 1회)
.\Run-VisualGGPK3.cmd   # 빌드 후 실행
# 또는
.\build-run.ps1      # 실행 중인 VisualGGPK3 종료 → Debug 빌드 → 실행
```

설정·즐겨찾기·진단 로그: `%AppData%\VisualGGPK3\`

개발/에이전트 인수인계: [`VISUALGGPK3-HANDOFF.md`](VISUALGGPK3-HANDOFF.md)

---

## VisualGGPK3

Eto.Forms + WPF 기반 GGPK/번들 뷰어. **2.9.24** 기준 주요 기능:

| 영역 | 기능 |
|------|------|
| **탐색** | GGPK 트리 + Bundles2 트리, 경로/제외어/타입 필터, 다중 선택(Ctrl/Shift) |
| **이미지** | DDS 등 미리보기, 줌/팬, UV 스프라이트 시퀀스(`NxM` 그리드), 필터 결과 PNG 일괄보내기 |
| **오디오** | WAV / OGG / MP3 재생 |
| **영상** | MP4 (LibVLC), BK2/Bink2 (`bink2w64.dll` — PoE 설치 경로 자동 탐색 또는 수동 지정) |
| **즐겨찾기** | **사용자 폴더**·드래그 정리, 파일·GGPK 폴더 등록·미리보기 이동 |
| **기타** | 최근 파일, **Update** 원클릭 자동 업데이트, 로드 상태 표시, 진단 로그, 레이아웃/필터 설정 저장 |

---

## Libraries

원본 LibGGPK3 구성은 그대로 포함됩니다.

### LibGGPK3
`Content.ggpk` 처리

### LibBundle3
`Bundles2` 디렉터리의 `*.bundle.bin` — Steam / Epic 클라이언트

### LibBundledGGPK3
LibGGPK3 + LibBundle3 통합 — 스탠드얼론 클라이언트

### Examples
- **VisualGGPK3** — 위 GUI (이 포크의 메인 앱)
- 기타 샘플 — 라이브러리 API 예제

---

## Overview

A cross-platform library for working with Content.ggpk from Path of Exile.  
Upstream rewrite of: https://github.com/aianlinb/LibGGPK2

This repository (**GGPK_Custom**) tracks upstream libraries and adds a maintained **VisualGGPK3** fork. It is **not** the official NuGet release channel for LibGGPK3.

## Notice

- Upstream license and attribution apply to LibGGPK3 / LibBundle3 / LibBundledGGPK3. Unauthorized modification, redistribution, or commercial use without complying with the upstream license is prohibited.
- Projects in this repository may not be fully thread-safe. Exercise caution when processing a single ggpk file from multiple threads.
- Updates do not guarantee forward compatibility. Review commit history before upgrading.
- **BK2 playback** requires `bink2w64.dll` from a PoE installation; it is not redistributed with this repo.
