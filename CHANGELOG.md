# Changelog

VisualGGPK3 (**GGPK_Custom** 포크) 변경 이력.  
GitHub **[Releases](https://github.com/Baegovda/GGPK_Custom/releases)** 와 동기화됩니다.

형식: [Keep a Changelog](https://keepachangelog.com/). 버전은 [Semantic Versioning](https://semver.org/) (VisualGGPK3 앱 버전).

---

## [2.8.6] — 2026-06-30

### Added
- **`CHANGELOG.md`** — GitHub에서 바로 보는 공개 변경 이력
- **`scripts/Publish-GitHubRelease.ps1`** — 태그 `vX.Y.Z` + GitHub Release 자동 게시
- README **Releases** 배지·CHANGELOG 링크

### Changed
- 버전업 지침에 **GitHub 동기화** 단계 추가 (handoff + `.cursor/rules`)

---

## [2.8.5] — 2026-06-30

### Added
- 왼쪽 **Favorites 목록 패널** (아이콘·파일명·폴더), 클릭 이동·우클릭/Delete 제거
- BK2 **수동 DLL 지정** (`bink2.txt`, Locate bink2w64.dll…)
- Daum/Kakao/Steam/Epic 등 **BK2 자동 탐색** 확장

### Fixed
- 즐겨찾기 선택 시 **프로그램 멈춤**
- 트리 **펼침/접힘·필터** 시 항목 겹침·불안정

### Changed
- 파일 크기 표시 **KiB/MiB → KB/MB**
- `README.md` GGPK_Custom 포크 소개
- 에이전트 **버전업 강제 규칙** (`.cursor/rules`)

---

## [2.8.0] — 2026-06-30

초기 GitHub 업로드. upstream LibGGPK3 2.7.5 기반 VisualGGPK3 커스텀.

### Added
- 경로/제외어/타입 **필터**, 필터 시 트리 상태 유지
- **이미지** 줌·팬·UV 스프라이트 시퀀스, **오디오** WAV/OGG/MP3
- **영상** MP4 (LibVLC), BK2 (bink2w64.dll)
- **즐겨찾기**·최근 파일, Ctrl/Shift **다중 선택**
- 필터 결과 **PNG 일괄 export**, **진단 로그**
- Space 재생, Right 다음 파일, 로드 상태 오버레이

[2.8.1]~[2.8.4] 상세는 `VISUALGGPK3-HANDOFF.md` 버전 이력 참고 (2.8.5에 통합 반영).

---

[2.8.6]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.8.6
[2.8.5]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.8.5
[2.8.0]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.8.0
