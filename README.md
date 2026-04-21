# ee-Purview-changer

Windows 11용 Microsoft Purview 라벨 변경 MVP 저장소입니다.

현재 구현은 **단건/검증 가능한 변경 흐름 + 실서비스용 로컬 라벨링 서비스 경계**에 집중한 초기 버전입니다.

## 현재 포함된 범위

- WPF 기반 Windows 11 데스크탑 앱 골격
- Microsoft 365 SSO/WAM 연동을 위한 MSAL 인증 스캐폴딩
- 파일 1건 선택 → 현재 상태 확인 → 대상 라벨 선택 → 변경 미리보기 → 감사 로그 기록 흐름
- Validation mode / Live mode 공용 UI 흐름과 로컬 파일 라벨링 서비스 경계
- Live mode용 MIP SDK 연동 지점 및 개발용 메타데이터 폴백
- Live mode 차단 상태 분류(비활성화/설정 누락/런타임 미준비/재조회 실패)
- Purview 기능별 REST API / SDK 지원 현황 표시
- 검증 모드(Validation mode) 기본 활성화

## Purview 기능 지원 현황

| 기능 | 우선 연동 방식 | 상태 | 비고 |
| --- | --- | --- | --- |
| 로컬 파일 현재 라벨 조회 | Microsoft Information Protection SDK | Preview | 서비스 경계 추가. 개발용 메타데이터 폴백으로 흐름 검증 가능 |
| 로컬 파일 라벨 변경 | Microsoft Information Protection SDK | Preview | 적용/재조회/감사 로그까지 연결. 실제 SDK 바인딩은 후속 작업 |
| Microsoft 365 클라우드 파일 라벨 조회/적용 | Microsoft Graph / Purview REST API | Planned | 클라우드 시나리오용 후속 범위 |
| 검증 모드 단건 흐름 | 로컬 검증 모드 | Supported | UI/확인/감사 로그 검증 가능 |
| 대량 변경 | 미지원 | Not supported | MVP 범위 제외 |

## 검증 모드

기본 설정은 `Ee.PurviewChanger.Desktop/appsettings.json`의 `validationMode.enabled=true` 입니다.

이 모드에서는:

- 현재 라벨을 시뮬레이션 값으로 표시합니다.
- 실제 Purview 라벨은 변경하지 않습니다.
- 변경 요청은 감사 로그 JSON으로만 기록됩니다.

즉, **운영 전 검증 가능한 단건 작업 흐름**을 먼저 점검할 수 있습니다.

## Live mode와 MIP SDK 연동 지점

`validationMode.enabled=false` 로 전환하면 Live mode 흐름이 활성화됩니다.

현재 Live mode 구현은 두 레이어로 나뉩니다.

- 공용 서비스 경계: 현재 상태 조회 / 라벨 변경 / 적용 후 재조회 / 감사 로그
- MIP SDK 연동 지점: `IMipSdkFileLabelClient` + `IMipSdkNativeBridge`

기본 제공 구현은 실제 SDK 대신 `mipSdk.developmentFallbackEnabled=true` 일 때
`App_Data/MipSdkMetadata` 에 개발용 메타데이터를 저장하여 Live mode UI/흐름을 검증합니다.

`mipSdk.developmentFallbackEnabled=false` 로 설정하면 `NativeMipSdkFileLabelClient` 경로가 활성화되며,
`mipSdk.nativeLibraryPath` 에 지정된 라이브러리에서 아래 UTF-8 JSON 내보내기 함수를 찾습니다.

- `EePurviewInspectLabelUtf8`
- `EePurviewApplyLabelUtf8`
- `EePurviewFreeUtf8Buffer`

즉, 현재 저장소는 **실제 SDK 연결을 수용하는 구조**와 **개발용 폴백 구현**을 함께 제공합니다.
실제 Microsoft Information Protection SDK 바인딩은 이 인터페이스를 대체하는 방식으로 이어서 구현하면 됩니다.

## Microsoft 365 인증 설정

`Ee.PurviewChanger.Desktop/appsettings.json`에서 아래 값을 설정하세요.

- `authentication.clientId`: Entra ID public client application ID
- `authentication.tenantId`: `organizations` 또는 tenant ID
- `authentication.useBroker`: Windows Broker/WAM 사용 여부
- `authentication.scopes`: 초기 로그인용 위임 권한 목록

현재 기본 스코프는 `User.Read`이며, 실제 Purview/Graph 호출 구현 시 필요한 권한을 추가해야 합니다.

Live mode 설정:

- `mipSdk.enabled`: Live mode 활성화 여부
- `mipSdk.applicationId`: 실제 SDK 전환 시 사용할 애플리케이션 식별자
- `mipSdk.nativeLibraryPath`: 실제 SDK 네이티브 라이브러리 경로 예약 값
- `mipSdk.developmentFallbackEnabled`: 개발용 폴백 사용 여부
- `mipSdk.developmentMetadataDirectory`: 개발용 라벨 메타데이터 저장소 경로
- `mipSdk.developmentDefaultLabel`: 저장 이력이 없을 때 초기 라벨로 간주할 값

Live mode 차단/실패 처리:

- `mipSdk.enabled=false` 이면 현재 상태 확인 단계에서 Live mode 비활성화 상태로 차단됩니다.
- `developmentFallbackEnabled=false` 인 경우 `applicationId`, `nativeLibraryPath`, Windows 실행 환경을 점검합니다.
  설정 누락과 실행 환경 미준비를 구분해 보여줍니다.
- 동일 라벨 재적용은 적용 전에 차단되며, 우회 호출이 들어와도 서비스가 다시 막습니다.
- 적용 후 재조회가 실패하거나 결과가 다르면 실패 상태와 감사 로그를 함께 남깁니다.

## 빌드

Linux에서 Windows 대상 프로젝트를 검증할 때:

```bash
dotnet build /home/runner/work/ee-Purview-changer/ee-Purview-changer/Ee.PurviewChanger.slnx -p:EnableWindowsTargeting=true
```

테스트 실행:

```bash
dotnet test /home/runner/work/ee-Purview-changer/ee-Purview-changer/Ee.PurviewChanger.slnx -p:EnableWindowsTargeting=true
```

Windows 11 single-file exe publish:

```bash
dotnet publish /home/runner/work/ee-Purview-changer/ee-Purview-changer/Ee.PurviewChanger.Desktop/Ee.PurviewChanger.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableWindowsTargeting=true
```

## GitHub Release에서 단일 실행 파일 확인

- `v*` 형식 태그(예: `v0.1.0`)를 push하면 GitHub Actions `release-single-file` 워크플로우가 실행됩니다.
- 워크플로우는 Windows 단일 파일 배포 결과를 zip(`ee-purview-changer-win11-single-file-<tag>.zip`)으로 묶어 GitHub Release 자산에 업로드합니다.
- GitHub 저장소의 **Releases** 페이지에서 해당 zip 자산을 내려받아 확인할 수 있습니다.

## 다음 구현 단계

1. `IMipSdkFileLabelClient`에 실제 Microsoft Information Protection SDK 바인딩 추가
2. Graph/Purview REST API 기반 클라우드 파일 라벨 조회/변경 추가
3. 실제 권한/인증 오류 분류와 재시도 정책 강화
4. 운영 배포/서명/설치 패키지 정리

## 추가 문서

- 현재 기능 / 앱 구조 / 추가 개발 제안: `docs/current-state-and-roadmap.md`
- Copilot 지속 개발 가이드: `.github/copilot-instructions.md`
