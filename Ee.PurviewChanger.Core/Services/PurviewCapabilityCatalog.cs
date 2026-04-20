using Ee.PurviewChanger.Core.Models;

namespace Ee.PurviewChanger.Core.Services;

public static class PurviewCapabilityCatalog
{
    public static IReadOnlyList<PurviewCapability> CreateDefault() =>
    [
        new(
            "로컬 파일 현재 라벨 조회",
            "Microsoft Information Protection SDK",
            SupportLevel.Preview,
            "실서비스용 조회/변경 서비스 경계를 도입했고, 개발용 메타데이터 폴백으로 흐름을 검증할 수 있습니다."),
        new(
            "로컬 파일 라벨 변경",
            "Microsoft Information Protection SDK",
            SupportLevel.Preview,
            "단건 변경 우선. 실제 SDK 전환 전에는 개발용 메타데이터 저장소를 통해 적용/재조회를 검증합니다."),
        new(
            "Microsoft 365 클라우드 파일 라벨 목록 조회",
            "Microsoft Graph / Purview REST API",
            SupportLevel.Planned,
            "클라우드 저장소 파일은 Graph 기반 라벨 조회/적용 후보로 관리합니다."),
        new(
            "검증 모드 단건 변경 흐름",
            "로컬 검증 모드",
            SupportLevel.Supported,
            "실서비스 연결 전에도 파일 선택, 상태 확인, 변경 미리보기, 감사 로그를 검증할 수 있습니다."),
        new(
            "대량 변경",
            "미지원",
            SupportLevel.NotSupported,
            "초기 버전 범위에서 제외합니다.")
    ];
}
