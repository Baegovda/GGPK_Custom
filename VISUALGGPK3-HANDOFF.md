# VisualGGPK3 — 에이전트 인수인계 · 버전 · 변경 이력

> **이 파일 하나**로 VisualGGPK3 커스텀 작업의 맥락, 버전 규칙, 변경 내역, 구조를 파악한다.  
> 새 에이전트/개발자는 **먼저 이 문서를 읽고** 작업한다.

---

## 현재 버전

| 항목 | 값 |
|------|-----|
| **솔루션 / 어셈블리** | `2.8.7` (`Directory.Build.props`) |
| **VisualGGPK3 창 제목** | `VisualGGPK3 (v2.8.7)` |
| **마지막 정리일** | 2026-06-30 |
| **GitHub 초기 커밋** | `2.8.0` (`9bdd6d8`) — 이후 `2.8.1`~`2.8.5`는 로컬 PATCH 누적 |

---

## 버전 올리기 지침 (에이전트 필수)

패치·기능 작업을 **마칠 때마다** 버전을 올리고, **이 문서의 [버전 이력](#버전-이력)** 과 **해당 버전 changelog** 를 갱신한다.

### 규칙

1. **항상 버전업** — 사용자에게 보이는 VisualGGPK3 변경이 있으면 반드시 올린다. (빌드 스크립트만 수정 등은 생략 가능)
2. **소소하게** — 한 번에 크게 점프하지 않는다.
   - **PATCH** `2.8.0` → `2.8.1` : 버그 수정, UI 미세 조정, 단일 작은 기능
   - **MINOR** `2.8.x` → `2.9.0` : 눈에 띄는 신규 기능 묶음, 미리보기/필터 등 영역 단위 추가
   - **MAJOR** `2.x` → `3.0.0` : 사용자 지시 또는 호환성 깨는 대규모 개편 시에만
3. **갱신 위치** (다섯 곳 + GitHub)
   - `Directory.Build.props` → `<Version>`
   - `.github/Version.txt` → **맨 위 줄**에 새 버전 추가
   - 이 파일 → [버전 이력](#버전-이력) + changelog 항목
   - **`CHANGELOG.md`** → `## [X.Y.Z]` 섹션 (GitHub 공개용, handoff와 동기화)
   - `README.md` — 배지·기능 표 (필요 시)
4. **GitHub Release** — push 후 `scripts/Publish-GitHubRelease.ps1` 실행  
   → `vX.Y.Z` 태그 + [Releases](https://github.com/Baegovda/GGPK_Custom/releases)에 CHANGELOG 본문 게시
5. **changelog 형식** — 버전별로:
   - 한 줄 요약
   - bullet으로 사용자 관점 변경점
   - (선택) 수정/추가된 주요 파일
5. **작업 마무리** — `dotnet build` 후, push 시 `Publish-GitHubRelease.ps1`.

### 버전을 올리지 않는 경우

- 주석·포맷만 변경
- 이 handoff 문서만 수정 (기능 코드 없음)
- upstream LibGGPK3 라이브러리만 수정하고 VisualGGPK3 UI/동작 무변경

---

## 버전 이력

> **2.8.0** = GitHub 초기 커밋. **공개 요약** → [`CHANGELOG.md`](CHANGELOG.md) · [Releases](https://github.com/Baegovda/GGPK_Custom/releases)

### 2.8.7 (2026-06-30) — 이미지 드래그 이동

- 스크롤바 없을 때도 좌클릭 드래그로 이미지 위치 이동 (`ZoomableImageView.panOffset`)
- 확대 시 기존 스크롤 팬 동작 유지

### 2.8.6 (2026-06-30) — GitHub 변경 이력 연동

- `CHANGELOG.md` — README/Releases와 동기화되는 공개 changelog
- `scripts/Publish-GitHubRelease.ps1` — 태그 + `gh release create`
- README Releases 배지·CHANGELOG 링크
- `version-bump-mandatory.mdc` — GitHub 동기화 단계 추가

### 2.8.5 (2026-06-30) — README · 버전업 강제 규칙

- `README.md` — GGPK_Custom 포크 소개, Quick start, 2.8.x 기능 표
- `.cursor/rules/version-bump-mandatory.mdc` — `alwaysApply: true` 버전·changelog 필수
- `.cursor/rules/visualggpk3-workflow.mdc` — 빌드·handoff 참조 정리

### 2.8.4 (2026-06-30) — BK2 재생 · 파일 크기 표시

- **BK2** — Daum/Kakao/Steam/Epic 등 경로 확장, 전 드라이브·`libraryfolders.vdf` 탐색 (`Bink2Locator.cs`)
- **수동 DLL** — `Bink2SettingsStore.cs` (`%AppData%\VisualGGPK3\bink2.txt`), 영상 플레이어 **Locate bink2w64.dll…** (`VideoPlayerView.cs`)
- **크기 라벨** — KiB/MiB → **KB/MB** (`MediaPreviewPanel.cs`, `MainWindow.FormatByteSize`)

### 2.8.3 (2026-06-30) — 즐겨찾기 목록 패널

- 상단 콤보박스(`FavoritesBar`) 제거 → **왼쪽 Favorites 목록** (`FavoritesPanel.cs`, `TreeItemIcons.cs`)
- 아이콘·파일명·폴더 컬럼, 클릭 이동, 우클릭/Delete 제거
- `favorites` 스플리터 너비 저장 (`LayoutSettingsStore.cs`, `MainWindow.TreesLayout`)

### 2.8.2 (2026-06-30) — 트리 펼침/접힘 · 필터 UI 안정화

- 펼침/접힘마다 `RefreshItem` (`DirectoryTreeItem.cs`)
- 필터 적용: 캐시 무효화 → 빈 폴더 접기 → **펼쳐진 폴더만** 갱신 (`TreeRefresh.cs`)
- 로딩 플레이스홀더 단일 인스턴스, WPF 가상화 `Recycling` → `Standard`
- 다중 선택 하이라이트 갱신 (`TreeMultiSelection.RefreshVisuals`)

### 2.8.1 (2026-06-30) — 즐겨찾기 이동 멈춤 수정

- `Index.TryGetFile` / `GGPK.Root.TryFindNode` 후 경로만 따라가기 (전체 트리 DFS 제거)
- `GGPKDirectoryTreeItem` / `BundleDirectoryTreeItem` — `FindChildDirectory` / `FindChildFile`
- 즐겨찾기 이동 루트→리프 순 펼침 (`FavoriteFileLocator.ExpandTo`)
- `favorite.navigate` 진단 로그 (`DiagnosticLog.Measure`)

---

### 2.8.0 (2026-06-30) — VisualGGPK3 대규모 UX/미리보기 패치

**요약:** 원본 2.7.5 대비 VisualGGPK3에 필터·미리보기·미디어 재생·즐겨찾기·단축키 등 실사용 기능 일괄 추가.

#### 필터 · 탐색

- **경로/파일명 검색** (`Show` 입력란) — `FileSearchFilter`, `TreeFilterBar`
- **제외 단어** (`Hide` 입력란) — `FileExcludeFilter`, 쉼표·공백·세미콜론 구분
- **파일 타입 프리셋** — Images / Text / Data / Audio / Video (`FileFormatFilter`)
- **필터 상태 유지** — 타입·제외어는 `%AppData%\VisualGGPK3\layout.txt`에 저장
- **필터 시 트리 유지** — 접기/선택/스크롤 초기화 없이 보이는 항목만 갱신 (`InvalidateFilterCacheDeep` + `RefreshDirectoryItem`)
- 필터 캐시 — `GGPKDirectoryTreeItem` / `BundleDirectoryTreeItem` (`_filterVersion`, `HasMatchingDescendant`)

#### 이미지 미리보기

- **통합 미리보기 패널** — `MediaPreviewPanel` (이미지·오디오·영상·상태)
- **줌/팬** — `ZoomableImageView` (휠 줌, 드래그 팬, 커서 기준 줌, % 배지)
- **작은 이미지 가운데 정렬**
- **파일 정보 오버레이** — 경로·크기·해상도 등; **Auto hide** 체크 + 호버 시 다시 표시 (`LayoutSettingsStore.infoAutoHide`)
- **UV 시퀀스 스프라이트 재생** — 파일명/경로의 `5x5`, `4x4` 등 `NxM` 그리드 자동 인식 (`UvSequenceGrid`, `SpriteSheetPlayer`, `SpriteSheetPlayerView`)

#### 오디오 미리보기

- **다중 포맷** — WAV, OGG, MP3 (`AudioPlayback` + NAudio / NAudio.Vorbis)
- **플레이어 UI** — 재생/일시정지, 정지, 시크, 볼륨, 메타데이터 (`AudioPlayerView`)
- **`.bank`** — FMOD bank는 정보만 표시, 재생 불가

#### 영상 미리보기

- **`.mp4`** — LibVLC (`VideoPlayback`, `VideoPlayerView`, `LibVLCSharp`, `LibVLCSharp.Eto`)
- **`.bk2` (Bink 2)** — VLC 불가; **`bink2w64.dll`** 네이티브 디코딩 (`Bink2Native`, `Bink2Playback`, `Bink2Locator`)
  - PoE 설치 폴더 또는 exe 옆에서 DLL 자동 탐색
  - 영상만 재생 (Bink 오디오 미구현)

#### 즐겨찾기 · 최근 파일

- **마지막 열 GGPK** — `%AppData%\VisualGGPK3\last.txt` (`RecentFileStore`)
- **즐겨찾기** — 추가/제거, 드롭다운 이동 (`FavoritesBar`, `FavoriteFilesStore`, `FavoriteFileLocator`, `FavoritePaths`)

#### 키보드

- **Space** — 오디오 / UV 스프라이트 / 영상 재생·일시정지
- **Right** — 필터 적용 후 보이는 다음 파일 (`TreeNavigation`)

#### 로딩 · 상태

- GGPK/번들 로드 시 **상세 상태 오버레이** (`LoadStatusReport`, `ShowStatus`)

#### 빌드 · 실행 (레포 루트)

- `setup.ps1` — .NET 8 SDK, oo2core 등
- `build-run.ps1` — 종료 → Debug 빌드 → 실행
- `Run-VisualGGPK3.cmd` — Release 실행

#### 신규 파일 (`Examples/VisualGGPK3/`)

| 파일 | 역할 |
|------|------|
| `TreeFilterBar.cs` | Show/Hide/타입 필터 UI |
| `FileSearchFilter.cs` / `FileExcludeFilter.cs` / `FileFormatFilter.cs` / `TreeViewFilter.cs` | 필터 로직 |
| `MediaPreviewPanel.cs` | 통합 미리보기 |
| `ZoomableImageView.cs` | 이미지 줌/팬 |
| `AudioPlayback.cs` / `AudioPlayerView.cs` | 오디오 |
| `VideoPlayback.cs` / `VideoPlayerView.cs` | MP4 (VLC) |
| `Bink2Native.cs` / `Bink2Playback.cs` / `Bink2Locator.cs` | BK2 (Bink) |
| `UvSequenceGrid.cs` / `SpriteSheetPlayer.cs` / `SpriteSheetPlayerView.cs` | UV 시퀀스 |
| `LayoutSettingsStore.cs` / `RecentFileStore.cs` | 설정·최근 파일 |
| `FavoritesBar.cs` / `FavoriteFilesStore.cs` / `FavoriteFileLocator.cs` / `FavoritePaths.cs` | 즐겨찾기 |
| `TreeNavigation.cs` | 트리 키보드 이동 |
| `LoadStatusReport.cs` | 로드 상태 텍스트 |

#### 수정된 주요 파일

| 파일 | 변경 요약 |
|------|-----------|
| `MainWindow.cs` | 필터바, 즐겨찾기, 미리보기 라우팅, 키보드, 이미지/오디오/영상 분기 |
| `TreeItems/GGPKDirectoryTreeItem.cs` | 필터 캐시, `EnumerateAllChildren` |
| `TreeItems/BundleDirectoryTreeItem.cs` | 동일 |
| `TreeItems/DirectoryTreeItem.cs` | `InvalidateFilterCache`, `EnumerateAllChildren` |
| `Program.cs` | `LibVLCSharp.Shared.Core.Initialize()` |
| `VisualGGPK3.csproj` | NAudio, Pfim, LibVLCSharp, LibVLCSharp.Eto, VideoLAN.LibVLC.* |

#### NuGet 추가 (VisualGGPK3)

- `NAudio`, `NAudio.Vorbis`, `Pfim`
- `LibVLCSharp`, `LibVLCSharp.Eto`, `VideoLAN.LibVLC.Windows` (OS별 Linux/Mac 패키지 조건부)

---

### 2.7.5 (upstream)

- LibGGPK3 / LibBundle3 / LibBundledGGPK3 원본
- VisualGGPK3 기본 골격만 존재 (미완성 안내 문구)

---

## 아키텍처 요약

```
MainWindow
├── TreeFilterBar
├── TreesLayout
│   ├── FavoritesPanel (즐겨찾기 목록)
│   └── MainLayout → GGPKTree / BundleTree → TreeViewFilter
└── RightLayout.Panel2
    ├── TextPanel (텍스트)
    └── MediaPreviewPanel
        ├── ZoomableImageView (이미지 / UV 프레임 / BK2 프레임)
        ├── AudioPlayerView
        ├── VideoPlayerView (VLC VideoView + BK2 분기)
        └── overlayBox (정보 + UV 플레이어 컨트롤)
```

### 사용자 설정 경로 (`%AppData%\VisualGGPK3\`)

| 파일 | 내용 |
|------|------|
| `layout.txt` | `main`, `inner`, `favorites`, `infoAutoHide`, `filterType`, `filterExclude` |
| `last.txt` | 마지막 GGPK 경로 |
| `favorites.txt` | 즐겨찾기 파일 경로 목록 |
| `bink2.txt` | 수동 지정 `bink2w64.dll` 경로 |
| `diagnostic.log` | 진단·성능·오류 로그 |

### 알려진 제한

- `TreeView` — Eto deprecated; 추후 `TreeGridView` 검토
- **BK2 오디오** — 미구현 (영상 프레임만)
- **BK2** — `bink2w64.dll` 필요 (PoE 설치 또는 exe 옆 복사)
- **LibVLC + Eto WPF** — WPF 백엔드에서 비디오 HWND 이슈 가능; MP4는 대체로 동작
- **DAT 미리보기** — LibDat3 미연동 (`DataFormat.Dat` TODO)
- upstream 라이브러리 스레드 안전성 주의 (README)

---

## 에이전트 작업 체크리스트

작업 시작 시:

1. 이 문서의 **현재 버전** 확인
2. `Examples/VisualGGPK3/` 및 `MainWindow.cs` 구조 파악
3. `.cursor/rules/version-bump-mandatory.mdc` — **버전업 필수** (alwaysApply)
4. `.cursor/rules/visualggpk3-workflow.mdc` — 빌드·실행

작업 종료 시:

1. 사용자-facing 변경 있으면 **버전 PATCH+1**
2. `Directory.Build.props`, `.github/Version.txt`, **이 문서**, **`CHANGELOG.md`** 갱신
3. `dotnet build` / `build-run.ps1` 확인
4. GitHub 반영: `git push` 후 `scripts/Publish-GitHubRelease.ps1`
5. changelog에 **무엇을 / 왜** 기록

---

## 빠른 실행

```powershell
# 최초 1회
powershell -ExecutionPolicy Bypass -File setup.ps1

# 개발: 빌드 + 실행
powershell -ExecutionPolicy Bypass -File build-run.ps1

# 또는 Release
.\Run-VisualGGPK3.cmd
```

---

## 대화 세션에서 사용자가 요청한 기능 (원문 맥락)

1. 파일 타입 필터 · 검색 UI  
2. 로드/파싱 상세 상태 오버레이  
3. 시작 시 마지막 파일 자동 열기  
4. 이미지 줌 (휠, 커서 기준, % 표시)  
5. 다중 포맷 오디오 + 풀 플레이어 UI  
6. 파일 정보 오버레이 자동 숨김 / 호버 / 설정 저장  
7. 필터 타입 세션 간 유지  
8. 작은 이미지 뷰포트 중앙 정렬  
9. 파일 즐겨찾기  
10. Space 재생, Right 다음 항목  
11. UV 시퀀스 (`5x5`, `4x4`) 스프라이트 재생  
12. 필터 **제외 단어** 입력  
13. 필터 변경 시 트리 상태 유지 (접기/선택 유지)  
14. 영상 미리보기 (mp4)  
15. BK2 재생 (`bink2w64.dll`)  
16. **이 handoff 문서 + 버전업 지침** (본 문서)
17. 즐겨찾기 콤보박스 멈춤 → 2.8.1 경로 조회 수정
18. 트리 펼침/필터 겹침 → 2.8.2 `TreeRefresh`
19. 즐겨찾기 왼쪽 목록 패널 → 2.8.3
20. BK2 Daum/수동 DLL, KB/MB 표시 → 2.8.4
21. README·버전업 강제 규칙 → 2.8.5

---

*다음 에이전트: 새 작업 후 위 [버전 올리기 지침](#버전-올리기-지침-에이전트-필수)에 따라 버전과 changelog만 이 파일에 이어 쓰면 된다.*
