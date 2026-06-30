# VisualGGPK3 — 에이전트 인수인계 · 버전 · 변경 이력

> **이 파일 하나**로 VisualGGPK3 커스텀 작업의 맥락, 버전 규칙, 변경 내역, 구조를 파악한다.  
> 새 에이전트/개발자는 **먼저 이 문서를 읽고** 작업한다.

---

## 현재 버전

| 항목 | 값 |
|------|-----|
| **솔루션 / 어셈블리** | `2.9.31` (`Directory.Build.props`) |
| **VisualGGPK3 창 제목** | `VisualGGPK3 (v2.9.31)` |
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
   → 인앱 업데이트용 zip: `Publish-GitHubRelease.ps1 -Package` (또는 `Package-VisualGGPK3Release.ps1` 후 `gh release upload`)
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

### 2.9.31 (2026-06-30) — 트리 점 잔상

- `TreeMultiSelection` — 마퀴 Adorner 제거 후 Invalidate, 1px 미만 박스 미그리기
- `WpfDarkTheme` — TreeViewItem 다크 템플릿(쉐브론만, 자식 없으면 확장 버튼 숨김)

### 2.9.30 (2026-06-30) — 미리보기 정보 오버레이

- `MediaPreviewPanel` — Path 별도 Label(한 줄), 상세 정보 분리, 오버레이 너비 72%·최대 560px

### 2.9.29 (2026-06-30) — 게임 실행 중 GGPK 경고

- `PoeGameDetector` — PoE 클라이언트 프로세스·게임 경로 감지
- 잠금 시 한국어 경고 대화상자 (게임 종료 또는 복사본 안내)

### 2.9.28 (2026-06-30) — GGPK 파일 잠금

- `GGPK.OpenFileStream` / `Index.OpenIndexStream` — ReadWrite 실패 시 Read+ShareReadWrite 폴백
- `MainWindow.CreateArchiveLoadError` — IOException 시 게임 실행 중 안내, 크래시 방지

### 2.9.27 (2026-06-30) — 상단 UI 레이아웃 수정

- `MainWindow` / `TreeFilterBar` — `TableLayout` → `DynamicLayout`, 상단·필터 영역 세로 늘어남 방지
- 타입 칩 2줄 배치 (All~Text / Data~Video)
- `WpfDarkTheme` — 전역 ScrollBar 스타일 제거, 버튼·TextBox 높이 고정

### 2.9.26 (2026-06-30) — 드래그 다중 선택 수정

- `TreeMultiSelection` — `SelectRange` 후 `ApplyVisuals` 호출
- 마퀴: 드래그 임계값 이후 시작, `CaptureMouse`/`FinishMarqueeSelection`으로 박스 잔류 방지
- `FindAdornerLayer` — 상위 시각 트리에서 AdornerLayer 탐색

### 2.9.25 (2026-06-30) — Favorites 별 아이콘

- `TreeItemIcons.FavoriteStar` — Favorites 패널 헤더 앞 금색 별

### 2.9.24 (2026-06-30) — 타입 필터 빈 폴더

- `HasFilteredVisibleChild` — 폴더 표시 여부를 필터된 자식과 동일 규칙으로 판단
- `Expandable` — 필터 활성 시 매칭 하위가 없으면 확장 아이콘 숨김
- `TreeRefresh.RefreshInitializedBranches` — 필터 변경 시 펼쳐진 가지 UI 동기화

### 2.9.23 (2026-06-30) — 트리 다중 선택

- `TreeItemIdentity` / `PathItemComparer` — 경로 기반 선택 집합 (WPF·Eto 인스턴스 불일치 해결)
- `TreeMultiSelection` — Explorer식 클릭·Shift 범위·Ctrl 토글·드래그 범위·마퀴 선택

### 2.9.22 (2026-06-30) — Right 키 트리 탐색

- `MainWindow.TryHandleRightArrow` — 접힌 폴더 펼치기, 펼친 폴더는 다음 보이는 항목, 파일은 다음 파일로 이동

### 2.9.21 (2026-06-30) — 정보 오버레이 크기

- `MediaPreviewPanel` — 파일 상세정보 박스 최대 너비 300px·높이 제한, 왼쪽 아래 고정; 로드 상태도 동일 스타일

### 2.9.20 (2026-06-30) — 인앱 자동 업데이트

- `AppUpdater` — `Version.txt` + GitHub Releases API로 최신 확인, zip 다운로드·robocopy 적용·재시작
- `MainWindow` — **Update** 툴바 버튼, 새 버전 시 `Update vX.Y.Z` 강조
- `Package-VisualGGPK3Release.ps1` / `Publish-GitHubRelease.ps1 -Package` — `VisualGGPK3-win-x64.zip` 업로드

### 2.9.19 (2026-06-30) — 슬라이더 UI

- `WpfDarkTheme` — 단일 트랙 슬라이더 템플릿 (볼륨/시크 이중 줄 제거)
- `VideoPlayerView` / `SpriteSheetPlayerView` — 슬라이더·버튼 테마 적용

### 2.9.18 (2026-06-30) — 다크 타이틀바 수정

- `WpfDarkTheme` — `Style.Add<FormHandler>` 전역 훅, HWND 준비 후 DWM 재적용·프레임 갱신

### 2.9.17 (2026-06-30) — 오디오 플레이어 UI

- `AudioPlayerView` — 카드형 레이아웃, 바 비주얼라이저, 재생 상태·볼륨 %, `AppTheme`/`WpfDarkTheme` 슬라이더

### 2.9.16 (2026-06-30) — 즐겨찾기 필터 우회

- `TreeViewFilter.RevealPath` — 즐겨찾기 이동 시 해당 경로·상위 폴더만 필터 무시하고 표시
- 필터 변경·Reset 시 reveal 해제

### 2.9.15 (2026-06-30) — WPF 트리 크래시

- `LoadFileAsync` — 백그라운드 로드 후 UI 갱신을 UI 스레드로 마샬링
- `DirectoryTreeItem` — lazy placeholder Count 제거, `RefreshItem` 지연
- `TreeRefresh` — 필터 갱신 refresh를 `AsyncInvoke`로 지연

### 2.9.14 (2026-06-30) — oo2core 배포

- `Directory.Build.targets` — `WinExe` 빌드에도 `oo2core.dll` 복사
- `Directory.Build.props` — 솔루션 없이 빌드해도 루트 `bin\Debug` 출력
- `MainWindow` — oo2core 누락 시 setup 안내

### 2.9.13 (2026-06-30) — UV 시퀀스 파일명 파싱

- `UvSequenceGrid` — 확장자 제거 후 `NxM` 매칭 (`sim_wispy_01_6x6.dds` 등)

### 2.9.12 (2026-06-30) — Auto hide 호버 복원

- `MediaPreviewPanel` — 숨김 후 정보 박스 영역 호버 시 다시 표시 (`MouseMove` 영역 감지, WPF 투명 패널 hit-test)

### 2.9.11 (2026-06-30) — UV Seq 필터

- `FileFormatFilter` / `TreeFilterBar` — **UV Seq** 칩 (`UvSequenceGrid` `NxM` 패턴 매칭)

### 2.9.10 (2026-06-30) — UV 시퀀스 자동 재생

- UV 시퀀스 파일 선택 시 프레임 애니메이션 자동 시작 (2프레임 이상)

### 2.9.9 (2026-06-30) — UV 시퀀스 원본 보기

- `MediaPreviewPanel` — UV 시퀀스 시 정보 박스 **View original** / **View sequence** 토글

### 2.9.8 (2026-06-30) — → 키 다음 파일 단일 선택

- `TrySelectNextVisibleItem` — `tree.SelectedItem` 동기화 (WPF·다중 선택 하이라이트 겹침 방지)
- 파일에서 → 시 `GetNextFileItem` (폴더 건너뛰고 다음 파일만)
- `TreeMultiSelection.SelectRange` — 경로 기반 인덱스 매칭

### 2.9.7 (2026-06-30) — 유형 필터 칩 UI

- `TreeFilterBar` — DropDown 제거, All/Images/Text/Data/Audio/Video 칩 버튼 (1클릭 선택, 강조색)

### 2.9.6 (2026-06-30) — 이미지 드래그 팬

- `ZoomableImageView` — `imageOrigin` 기준 팬; 스크롤 유무와 관계없이 드래그 이동

### 2.9.5 (2026-06-30) — 즐겨찾기 클릭 즉시 미리보기

- `FavoritesPanel` — 마우스 클릭 시 `FavoriteSelected` 즉시 발생 (WPF `PreviewMouseLeftButtonDown`)
- 키보드 선택은 기존 `SelectionChanged` 유지

### 2.9.4 (2026-06-30) — 스크롤바 다크 테마

- `WpfDarkTheme` — 앱 전역 `ScrollBar`/`ScrollViewer` 스타일 (트랙 `#1e1e26`, 썸 `#363642`)

### 2.9.3 (2026-06-30) — 타이틀 바 다크 테마

- `WpfDarkTheme` — DWM `USE_IMMERSIVE_DARK_MODE` + Win11 캡션/텍스트/테두리 색 (`#141418`, `#ececf0`)
- 메인 창·입력 다이얼로그 타이틀 바가 앱 다크 팔레트와 일치

### 2.9.2 (2026-06-30) — 다크 테마 UI

- `AppTheme.cs` / `WpfDarkTheme.cs` — 통일 색상 팔레트, Eto+WPF 1회 스타일 적용
- 상단 Open/경로/필터 바 리디자인 (`TreeFilterBar`)
- 트리·즐겨찾기·스플리터·선택 하이라이트 다크 톤

### 2.9.1 (2026-06-30) — → 키 아래 이동 복구

- `TreeNavigation.ShouldExpandOnRightArrow` — 접힌 폴더 Right 시 펼치기 판별
- 더 내려갈 수 없을 때만 다음 보이는 항목으로 이동 (파일·빈 폴더 포함)
- 경로 기반 항목 매칭으로 트리 갱신 후에도 동작

### 2.9.0 (2026-06-30) — 즐겨찾기 사용자 폴더

- `FavoritesStore` + `favorites.json` ( `favorites.txt` 자동 마이그레이션)
- 즐겨찾기 패널 **트리 UI**: 사용자 폴더 생성/이름 변경/삭제 (`+ Folder`, 우클릭)
- 항목·폴더 **드래그 앤 드롭** 이동 (Windows, `FavoritesTreeDragDrop`)
- 우클릭 **Move to** 서브메뉴
- 폴더 선택 시 **Add to favorites** 가 해당 폴더에 추가

### 2.8.9 (2026-06-30) — 즐겨찾기 미리보기

- 즐겨찾기 목록에서 파일 선택 시 `OnSelectionChanged` 호출로 **뷰포트 미리보기** 즉시 갱신

### 2.8.8 (2026-06-30) — 폴더 즐겨찾기

- 트리에서 **폴더** 우클릭 → Add to favorites / Remove from favorites
- 즐겨찾기 목록에 폴더 아이콘·이름 표시, 클릭 시 해당 폴더로 트리 이동·선택
- `FavoritePaths.IsDirectory`, `FavoriteFileLocator.FindDirectory`, `FindDirectoryByPath` (GGPK/번들)

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
- **즐겨찾기** — `FavoritesPanel`, `FavoritesStore`, `FavoriteFileLocator`, `FavoritePaths`, `FavoriteTreeItems`

#### 키보드

- **Space** — 오디오 / UV 스프라이트 / 영상 재생·일시정지
- **Right** — 접힌 폴더 펼치기; 펼친 폴더는 다음 보이는 항목; 파일은 다음 **파일** (`TryHandleRightArrow`)

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
| `FavoritesPanel.cs` / `FavoritesStore.cs` / `FavoriteFileLocator.cs` / `FavoritePaths.cs` / `FavoriteTreeItems.cs` | 즐겨찾기 |
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
