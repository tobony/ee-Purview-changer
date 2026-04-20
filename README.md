# ee-Purview-changer

Windows 11용 Microsoft Purview 라벨 변경 MVP 저장소입니다.

현재 구현은 **단건/검증 가능한 변경 흐름**에 집중한 초기 버전입니다.

## 현재 포함된 범위

- WPF 기반 Windows 11 데스크탑 앱 골격
- Microsoft 365 SSO/WAM 연동을 위한 MSAL 인증 스캐폴딩
- 파일 1건 선택 → 현재 상태 확인 → 대상 라벨 선택 → 변경 미리보기 → 감사 로그 기록 흐름
- Purview 기능별 REST API / SDK 지원 현황 표시
- 검증 모드(Validation mode) 기본 활성화

## Purview 기능 지원 현황

| 기능 | 우선 연동 방식 | 상태 | 비고 |
| --- | --- | --- | --- |
| 로컬 파일 현재 라벨 조회 | Microsoft Information Protection SDK | Planned | 정확한 조회는 SDK 연동 필요 |
| 로컬 파일 라벨 변경 | Microsoft Information Protection SDK | Planned | 실제 적용은 SDK 연동 후 활성화 |
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

## Microsoft 365 인증 설정

`Ee.PurviewChanger.Desktop/appsettings.json`에서 아래 값을 설정하세요.

- `authentication.clientId`: Entra ID public client application ID
- `authentication.tenantId`: `organizations` 또는 tenant ID
- `authentication.useBroker`: Windows Broker/WAM 사용 여부
- `authentication.scopes`: 초기 로그인용 위임 권한 목록

현재 기본 스코프는 `User.Read`이며, 실제 Purview/Graph 호출 구현 시 필요한 권한을 추가해야 합니다.

## 빌드

Linux에서 Windows 대상 프로젝트를 검증할 때:

```bash
dotnet build /home/runner/work/ee-Purview-changer/ee-Purview-changer/Ee.PurviewChanger.slnx -p:EnableWindowsTargeting=true
```

테스트 실행:

```bash
dotnet test /home/runner/work/ee-Purview-changer/ee-Purview-changer/Ee.PurviewChanger.slnx -p:EnableWindowsTargeting=true
```

## 다음 구현 단계

1. MIP SDK 연동으로 로컬 파일 현재 라벨 조회/변경 활성화
2. Graph/Purview REST API 기반 클라우드 파일 라벨 조회/변경 추가
3. 실제 결과 재조회와 예외 처리 강화
4. 운영 배포/서명/설치 패키지 정리
