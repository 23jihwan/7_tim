using UnityEngine;

// 맵 생성기가 제대로 동작하는지 Console 창으로 확인하는 테스트용 스크립트
public class MapTester : MonoBehaviour
{
    public MapConfig config;

    [Tooltip("1 ~ 3층")]
    [Range(1, 3)] public int floor = 1;

    public int seed = 1024;

    [Tooltip("체크하면 실행할 때마다 다른 시드로 생성")]
    public bool useRandomSeed = true;

    private void Start()
    {
        GenerateAndPrint();
    }

    // 컴포넌트 오른쪽 점 세 개 메뉴에서 Play 없이도 실행할 수 있다
    [ContextMenu("맵 생성 테스트")]
    public void GenerateAndPrint()
    {
        if (config == null)
        {
            Debug.LogWarning("MapTester: Config 칸에 MapConfig 에셋을 넣어주세요.");
            return;
        }

        if (useRandomSeed) seed = Random.Range(1000, 10000);

        MapData map = MapGenerator.Generate(config, floor - 1, seed);
        Debug.Log(MapGenerator.ToDebugString(map));
    }
}