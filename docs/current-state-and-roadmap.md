# ee-Purview-changer 현재 기능 / 구조 / 추가 개발 제안

## 1. 현재 앱이 제공하는 기능

이 저장소의 현재 버전은 **Windows 11용 Microsoft Purview 단건 라벨 변경 MVP**입니다.  
핵심 목적은 대량 처리보다 **단건 파일 기준의 검증 가능한 변경 흐름**을 먼저 확보하는 것입니다.

현재 앱에서 확인 가능한 기능은 아래와 같습니다.

- Microsoft 365 SSO/WAM 기반 로그인 시도
- 로컬 파일 1건 선택
- 현재 상태 확인
- 대상 라벨 선택
- 라벨 변경 미리보기
- 변경 적용 또는 시뮬레이션 기록
- 감사 로그(JSON) 생성 및 로그 폴더 열기
- Purview 기능 지원 현황 확인

지원 파일 형식은 현재 설정 기준으로 아래와 같습니다.

- `.docx`
- `.xlsx`
- `.pptx`
- `.pdf`
- `.txt`

후보 라벨은 현재 설정 기준으로 아래 3개가 기본 제공됩니다.

- General
- Confidential
- Highly Confidential

## 2. 실행 모드 기준 현재 동작

### Validation mode

기본 설정은 `Ee.PurviewChanger.Desktop/appsettings.json`의 `validationMode.enabled=true` 입니다.

이 모드에서는:

- 현재 라벨을 시뮬레이션 값으로 표시합니다.
- 실제 Purview 라벨은 변경하지 않습니다.
- 변경 요청은 감사 로그로만 남깁니다.
- UI 흐름, 예외 상태, 미리보기, 감사 로그 경로를 검증할 수 있습니다.

즉, **운영 연결 전에도 앱의 주요 사용자 흐름을 확인할 수 있는 모드**입니다.

### Live mode

`validationMode.enabled=false` 이면 Live mode 흐름이 활성화됩니다.

Live mode는 현재 두 가지 방식으로 분기됩니다.

1. **개발용 폴백**
   - `mipSdk.developmentFallbackEnabled=true`
   - 실제 SDK 대신 `App_Data/MipSdkMetadata` 에 저장되는 메타데이터로 라벨 조회/변경 흐름을 검증합니다.

2. **네이티브 브리지 경로**
   - `mipSdk.developmentFallbackEnabled=false`
   - 실제 네이티브 라이브러리를 통해 아래 함수 연결을 기대합니다.
     - `EePurviewInspectLabelUtf8`
     - `EePurviewApplyLabelUtf8`
     - `EePurviewFreeUtf8Buffer`

현재 Live mode에서 앱은 아래 상태를 구분해서 보여줄 수 있습니다.

- `mipSdk.enabled=false`
- `mipSdk.applicationId` 누락
- `mipSdk.nativeLibraryPath` 누락
- Windows 실행 환경 미준비
- 네이티브 라이브러리 누락
- 동일 라벨 재적용 차단
- 적용 실패
- 적용 후 재조회 실패

## 3. 현재 앱 구조

솔루션은 3개 프로젝트로 구성됩니다.

### `Ee.PurviewChanger.Desktop`

WPF 데스크탑 앱 프로젝트입니다.

주요 책임:

- 메인 화면 렌더링
- 사용자 입력 처리
- 인증 상태 표시
- 서비스 호출 조합
- 실행 모드 배너 및 상태 메시지 표시

주요 파일:

- `Ee.PurviewChanger.Desktop/MainWindow.xaml`
- `Ee.PurviewChanger.Desktop/MainWindow.xaml.cs`
- `Ee.PurviewChanger.Desktop/Services/AppOptionsLoader.cs`
- `Ee.PurviewChanger.Desktop/Services/Microsoft365AuthenticationService.cs`
- `Ee.PurviewChanger.Desktop/appsettings.json`

### `Ee.PurviewChanger.Core`

도메인 모델과 핵심 서비스 계층입니다.

주요 책임:

- 파일 상태 확인
- 변경 미리보기 생성
- 라벨 변경 실행
- 감사 로그 기록
- Purview 기능 카탈로그 제공
- Live mode용 MIP SDK 추상화 및 네이티브 브리지 경계 제공

주요 서비스 흐름:

- `IFileInspectionService`
  - `LocalFileInspectionService`
  - `MipSdkFileInspectionService`
- `ILabelChangeService`
  - `ValidationModeChangeService`
  - `MipSdkLabelChangeService`
- `IMipSdkFileLabelClient`
  - `DevelopmentMipSdkFileLabelClient`
  - `NativeMipSdkFileLabelClient`
- `MipSdkFileLabelClientFactory`
- `LabelChangePlanner`
- `AuditLogService`

### `Ee.PurviewChanger.Core.Tests`

MSTest 기반 단위 테스트 프로젝트입니다.

현재 검증 중인 주요 영역:

- 라벨 변경 미리보기 규칙
- Validation mode 파일 점검 동작
- Live mode 서비스 흐름
- MIP SDK 클라이언트 팩토리 분기
- Native client 가드레일

## 4. 현재 구조의 핵심 설계 포인트

### UI와 라벨링 구현의 분리

UI는 `MainWindow` 에 모여 있지만, 실제 라벨링 관련 판단은 Core 서비스로 분리되어 있습니다.  
앞으로 기능이 커져도 WPF 화면 코드보다 Core 계층 확장을 우선하면 유지보수가 쉬워집니다.

### 실행 모드 분기 유지

현재 구조는 Validation mode와 Live mode를 동일한 UI 흐름에서 처리합니다.  
이 구조 덕분에 실 SDK 연결 전에도 사용자 경험과 상태 전이를 빠르게 검증할 수 있습니다.

### MIP SDK 연동 경계 확보

실제 라벨 조회/변경은 `IMipSdkFileLabelClient` 뒤로 숨겨져 있습니다.  
따라서 향후 실제 Microsoft Information Protection SDK 바인딩을 추가하더라도 UI와 상위 서비스 변경을 최소화할 수 있습니다.

### 실패 상태의 구체적 표현

`FileInspectionStatus` 와 `LabelChangeStatus` 로 Live mode 실패 원인을 구분하고 있습니다.  
향후 운영 품질을 높이려면 이 상태 체계를 유지하면서 오류 분류만 더 세분화하는 방식이 적합합니다.

## 5. 현재 기준 한계

현재 저장소는 MVP 범위에 맞춰 아래 사항이 아직 제한됩니다.

- 실제 MIP SDK 완전 바인딩 미완료
- Microsoft Graph / Purview REST 기반 클라우드 파일 처리 미구현
- 대량 변경 미지원
- UI가 단일 Window 중심이라 화면 단위 확장성은 아직 낮음
- 감사 로그가 로컬 JSON 중심이라 운영 관제 연동은 미구현
- 권한 실패 / 정책 충돌 / 네트워크 오류 분류가 아직 제한적

## 6. 추가 개발 제안

### 우선순위 1: 실제 로컬 파일 라벨 처리 완성

- `IMipSdkFileLabelClient` 기반으로 실제 MIP SDK 바인딩 구현
- 네이티브 브리지 응답 규격 고정
- 적용 후 재조회 검증 강화
- 파일 잠금, 권한 부족, 읽기 전용 파일 등의 오류 분류 추가

### 우선순위 2: 클라우드 파일 시나리오 확장

- Microsoft Graph / Purview REST API 기반 파일 조회/변경 기능 추가
- 로컬 파일과 클라우드 파일을 분리된 서비스로 관리
- 저장소 유형(로컬/SharePoint/OneDrive)별 상태 모델 정리

### 우선순위 3: UI 구조 개선

- MainWindow 중심 코드를 ViewModel 또는 화면 단위 서비스로 분리
- 상태 메시지와 액션 가능 여부를 바인딩 중심으로 정리
- 설정 화면과 실행 화면을 분리

### 우선순위 4: 운영성 강화

- 감사 로그 스키마 버전 관리
- 결과 상태 집계 및 실패 분석용 로그 확장
- 설정 검증 화면 추가
- 진단 모드 및 환경 점검 체크리스트 추가

### 우선순위 5: 테스트 확대

- 네이티브 브리지 응답 실패 케이스 추가
- 감사 로그 출력 검증 강화
- 설정값 조합별 상태 전이 테스트 추가
- 인증 서비스의 구성/실패 시나리오 테스트 추가

## 7. 지속 개발 시 권장 원칙

- UI에서 라벨링 세부 로직을 직접 구현하지 말고 Core 서비스로 내릴 것
- Live mode 확장은 `IMipSdkFileLabelClient` 또는 별도 클라우드 전용 인터페이스 뒤에서 구현할 것
- 새 기능을 추가할 때 Validation mode 흐름도 함께 유지할 것
- 상태 분기 추가 시 `FileInspectionStatus`, `LabelChangeStatus` 와 사용자 메시지를 함께 설계할 것
- 감사 로그는 기능 변경 시 항상 함께 검토할 것
- 단건 흐름을 깨지 않는 범위에서 기능을 확장할 것

## 8. 빌드 및 테스트

빌드:

```bash
dotnet build /home/runner/work/ee-Purview-changer/ee-Purview-changer/Ee.PurviewChanger.slnx -p:EnableWindowsTargeting=true
```

테스트:

```bash
dotnet test /home/runner/work/ee-Purview-changer/ee-Purview-changer/Ee.PurviewChanger.slnx -p:EnableWindowsTargeting=true
```
