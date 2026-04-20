using Ee.PurviewChanger.Core.Models;

namespace Ee.PurviewChanger.Core.Services;

public static class PurviewCapabilityCatalog
{
    public static IReadOnlyList<PurviewCapability> CreateDefault() =>
    [
        new(
            "로컬 파일 현재 라벨 조회",
            "Microsoft Information Protection SDK",
            SupportLevel.Planned,
            "정확한 현재 상태 조회와 변경에는 MIP SDK 연동이 필요합니다."),
        new(
            "로컬 파일 라벨 변경",
            "Microsoft Information Protection SDK",
            SupportLevel.Planned,
            "단건 변경 우선. 실제 라벨 적용은 Windows 데스크탑에서 SDK로 연결해야 합니다."),
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
