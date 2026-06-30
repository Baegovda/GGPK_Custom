# Changelog

VisualGGPK3 (**GGPK_Custom** 포크) 변경 이력.  
GitHub **[Releases](https://github.com/Baegovda/GGPK_Custom/releases)** 와 동기화됩니다.

형식: [Keep a Changelog](https://keepachangelog.com/). 버전은 [Semantic Versioning](https://semver.org/) (VisualGGPK3 앱 버전).

---

## [2.9.31] — 2026-06-30

### Fixed
- 트리 빈 공간에 흰 점(잔상)이 남던 문제 — 드래그 선택 박스 정리 강화, 트리 확장 아이콘·연결선 기본 템플릿 교체

---

## [2.9.30] — 2026-06-30

### Fixed
- 이미지 미리보기 정보 오버레이(Path·Auto hide) 레이아웃 깨짐 — 경로 한 줄 표시, 패널 너비 확대, File/Size 등 분리

---

## [2.9.29] — 2026-06-30

### Fixed
- 게임 실행 중 `Content.ggpk` 잠금 시 **「게임 실행 중 — GGPK를 열 수 없음」** 경고 대화상자 표시

---

## [2.9.28] — 2026-06-30

### Fixed
- 게임 실행 중 `Content.ggpk` 열 때 앱이 크래시하던 문제 — 파일 잠금 시 안내 대화상자, 읽기 전용 폴백 시도
- 오류 처리 중 `GGPK`를 다시 열며 예외가 재발하던 버그 수정

---

## [2.9.27] — 2026-06-30

### Fixed
- 상단 툴바·필터 바 버튼/입력칸 배열 깨짐 — `DynamicLayout`으로 재구성, 필터 패널이 세로로 늘어나던 문제 수정
- WPF 전역 스크롤바 스타일 제거 (Eto 레이아웃 측정 오류 유발)
- 버튼·입력칸 높이 30px 통일

---

## [2.9.26] — 2026-06-30

### Fixed
- 드래그 다중 선택 시 항목이 선택되지 않던 문제 — 범위 선택 후 하이라이트 갱신 누락 수정
- 드래그 선택 박스(마퀴)가 화면에 남던 문제 — 마우스 캡처·해제 시 정리, 임계 거리 이후에만 박스 표시

---

## [2.9.25] — 2026-06-30

### Changed
- Favorites 패널 헤더 앞에 **별 아이콘** 추가

---

## [2.9.24] — 2026-06-30

### Fixed
- 타입 필터 시 해당 파일이 **없는 빈 폴더**가 트리에 남던 문제 — 폴더 표시를 필터된 자식 기준으로 통일, 펼침 없는 폴더는 확장 아이콘 숨김

---

## [2.9.23] — 2026-06-30

### Fixed
- 트리 **Shift/Ctrl 다중 선택** — 경로 기반 동등성으로 범위 선택이 2개로 잘리던 문제 수정 (Windows 탐색기와 동일)

### Added
- 항목 위 **드래그**로 범위 선택, 빈 영역 **드래그**로 끈적이 박스(마퀴) 다중 선택
- **Ctrl+Shift** — 기존 선택에 범위 추가

---

## [2.9.22] — 2026-06-30

### Fixed
- **Right** 방향키 — 접힌 폴더는 펼치기, 펼친 폴더/파일은 다음 항목, 파일(최하위)은 다음 **파일**로 이동·미리보기

---

## [2.9.21] — 2026-06-30

### Fixed
- 이미지 미리보기 **파일 상세정보** 오버레이가 과하게 커지던 문제 — 왼쪽 아래 컴팩트 박스(최대 300px)로 복원, 긴 내용은 스크롤

---

## [2.9.20] — 2026-06-30

### Added
- **Update** 버튼 — GitHub Release에서 최신 `VisualGGPK3-win-x64.zip` 자동 다운로드·설치·재시작
- `AppUpdater` — `Baegovda/GGPK_Custom` 버전 확인 및 Windows 인앱 업데이트
- `scripts/Package-VisualGGPK3Release.ps1` — 릴리스 zip 패키징 (`Publish-GitHubRelease.ps1 -Package`)

---

## [2.9.19] — 2026-06-30

### Fixed
- 슬라이더 아래 빈 줄이 겹쳐 보이던 WPF 렌더링 문제 (이퀄라이저 아님) — 다크 슬라이더 템플릿 적용

---

## [2.9.18] — 2026-06-30

### Fixed
- Windows 다크 **타이틀바** 미적용 — Eto `FormHandler` 생성 시점 훅 + `Loaded`/`SourceInitialized` 재적용

---

## [2.9.17] — 2026-06-30

### Changed
- 오디오 플레이어 UI 개편 — 카드 레이아웃, 비주얼라이저, 상태·볼륨 표시, 다크 테마 슬라이더

---

## [2.9.16] — 2026-06-30

### Fixed
- 즐겨찾기 클릭 시 활성 필터에 가려져 트리·미리보기가 안 되던 문제 — 해당 경로를 일시적으로 표시 (`RevealPath`)

---

## [2.9.15] — 2026-06-30

### Fixed
- GGPK 로드·필터 적용 시 WPF 트리 `ItemsControl is inconsistent` 크래시 (UI 스레드 밖 트리 갱신, lazy Count 불일치)

---

## [2.9.14] — 2026-06-30

### Fixed
- `oo2core.dll`이 VisualGGPK3(`WinExe`) 빌드 출력에 복사되지 않아 PoE2 번들 GGPK 로드 실패하던 문제
- 단일 프로젝트 빌드 시 출력 경로가 `bin\Debug`로 통일되도록 수정
- `oo2core` 누락 시 안내 메시지 개선

---

## [2.9.13] — 2026-06-30

### Fixed
- UV 시퀀스 파일명 `*_6x6.dds` 등 확장자 바로 앞 `NxM` 패턴이 인식되지 않던 문제

---

## [2.9.12] — 2026-06-30

### Fixed
- 이미지 미리보기 **Auto hide** 후 정보 박스 위치에 마우스를 올려도 다시 표시되지 않던 문제

---

## [2.9.11] — 2026-06-30

### Added
- 파일 유형 필터 **UV Seq** — 이름/경로에 `NxM` 그리드가 있는 UV 시퀀스만 표시

---

## [2.9.10] — 2026-06-30

### Changed
- UV 시퀀스 파일 선택 시 **자동 재생** 시작

---

## [2.9.9] — 2026-06-30

### Added
- UV 시퀀스 미리보기 정보 박스 **View original** 버튼 — 전체 스프라이트 시트 원본 보기 / 시퀀스 복귀

---

## [2.9.8] — 2026-06-30

### Fixed
- **→(오른쪽)** 키: 파일에서 다음 **파일**만 단일 선택 (다중 선택·폴더 건너뜀, Eto 선택 동기화)

---

## [2.9.7] — 2026-06-30

### Changed
- 파일 유형 필터: 콤보박스 → **칩 버튼** (All / Images / Text … 한 번 클릭 선택)

---

## [2.9.6] — 2026-06-30

### Changed
- 이미지 미리보기: 스크롤바 없을 때도 **드래그로 이동** 가능 (뷰포트 기준 팬)

---

## [2.9.5] — 2026-06-30

### Fixed
- 즐겨찾기 **한 번 클릭**으로 파일 트리와 동일하게 즉시 미리보기 표시

---

## [2.9.4] — 2026-06-30

### Changed
- **스크롤바** 다크 테마 적용 (트랙·썸 색상, 트리·텍스트·목록 등 앱 전역)

---

## [2.9.3] — 2026-06-30

### Changed
- Windows **타이틀 바** 다크 테마 적용 (DWM: 어두운 모드 + 창 배경·텍스트·테두리 색상)

---

## [2.9.2] — 2026-06-30

### Changed
- **다크 테마** UI 전면 적용 (`AppTheme`, `WpfDarkTheme`)
- 상단 툴바·필터 바 레이아웃 정리, 버튼/입력칸 스타일 통일
- 트리·즐겨찾기·텍스트 패널 다크 배경·가독성 개선 (성능 영향 없음, 스타일 1회 적용)

---

## [2.9.1] — 2026-06-30

### Fixed
- **→(오른쪽)** 키: 더 들어갈 수 없을 때(파일·빈 폴더 등) 아래 항목으로 이동 복구
- 펼칠 하위 폴더가 있으면 기존처럼 펼침/하위 진입 우선

---

## [2.9.0] — 2026-06-30

### Added
- 즐겨찾기 **사용자 폴더** 생성·이름 변경·삭제 (`+ Folder`, 우클릭)
- 즐겨찾기 항목·폴더 **드래그로 이동** (Windows)
- 우클릭 **Move to** 메뉴 (드래그 대체)
- `favorites.json` 저장 (기존 `favorites.txt` 자동 마이그레이션)

---

## [2.8.9] — 2026-06-30

### Fixed
- 즐겨찾기에서 **파일** 선택 시 트리 이동과 함께 **미리보기 뷰포트**에 바로 표시

---

## [2.8.8] — 2026-06-30

### Added
- 즐겨찾기에 **폴더** 추가·이동 (트리에서 폴더 우클릭 → Add to favorites, 목록에 폴더 아이콘)

---

## [2.8.7] — 2026-06-30

### Changed
- 이미지 미리보기: 스크롤바가 없어도 **드래그로 화면 이동** (작은 이미지·맞춤 보기)

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

[2.9.31]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.31
[2.9.30]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.30
[2.9.29]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.29
[2.9.28]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.28
[2.9.27]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.27
[2.9.26]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.26
[2.9.25]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.25
[2.9.24]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.24
[2.9.23]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.23
[2.9.22]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.22
[2.9.21]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.21
[2.9.20]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.20
[2.9.11]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.11
[2.9.10]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.10
[2.9.9]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.9
[2.9.8]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.8
[2.9.7]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.7
[2.9.6]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.6
[2.9.5]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.5
[2.9.4]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.4
[2.9.3]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.3
[2.9.2]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.2
[2.9.1]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.1
[2.9.0]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.9.0
[2.8.9]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.8.9
[2.8.8]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.8.8
[2.8.7]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.8.7
[2.8.6]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.8.6
[2.8.5]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.8.5
[2.8.0]: https://github.com/Baegovda/GGPK_Custom/releases/tag/v2.8.0
