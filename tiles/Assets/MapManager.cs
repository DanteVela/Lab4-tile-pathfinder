using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Custom struct to hold the per-tile information needed for the A* pathing search
public struct grid_cell
{
    public bool visited;
    public bool isBlocked;
    public Vector2Int parent;
    public float g, h, f;
}

public class MapManager : MonoBehaviour
{
    // fixed map size for simplicity - map file must match
    public const int WIDTH = 12;
    public const int HEIGHT = 12;

    // need to know tile size (color for debugging)
    private Vector2 TILE_SIZE;
    private Color TILE_COLOR;

    // prefab tiles (and debugging labels)
    public GameObject[] _tilePrefabs;
    public GameObject _labelPrefab;
    public GameObject _uiCanvas;

    // A* pathfinding array and queue
    private grid_cell[,] _map = new grid_cell[WIDTH, HEIGHT];
    private List<KeyValuePair<float, Vector2Int>> _openList = new List<KeyValuePair<float, Vector2Int>>();

    // references to tile spriterenderers and tile labels for debugging
    private SpriteRenderer[,] _tiles = new SpriteRenderer[WIDTH, HEIGHT];
    private Text[,] _labels = new Text[WIDTH, HEIGHT];

    // steps is a convenient way to generate the 8 children of a grid square
    private Vector2Int[] _steps = new Vector2Int[8];

    void Start()
    {
        TILE_SIZE = _tilePrefabs[0].transform.GetComponent<SpriteRenderer>()
                            .bounds.extents * 2;

        // load map and create tiles
        readMapFile();

        // for debugging, store the color of an unblocked tile (for changing them back)
        TILE_COLOR = _tiles[0,0].color;

        // store "step" vectors for the four cardinal directions
        _steps[0].x = -1; _steps[0].y =  0;
        _steps[1].x =  0; _steps[1].y = -1;
        _steps[2].x =  0; _steps[2].y =  1;
        _steps[3].x =  1; _steps[3].y =  0;

        // and the four diagonal directions
        _steps[4].x =  1; _steps[4].y =  1;
        _steps[5].x =  1; _steps[5].y = -1;
        _steps[6].x = -1; _steps[6].y =  1;
        _steps[7].x = -1; _steps[7].y = -1;

    }

    /*********************************************************************************************
        Collision with blocked tiles (walls)
        - called by agents
        - returns a response vector indicating the amount to "push" the agent out of the walls
        - (0,0) indicates no collisions happening
     */

    public Vector2 checkBlockedCollision(Vector2 pos, Vector2 extents)
    {
        // convert the world position that we're checking to the coordinates of a grid cell
        Vector2Int posGC = vectorToGC(pos);

        // loop over the diagonal steps to check the diagonally-adjacent cells
        // check those first, because a diagonal collision implies two cardinal direction collisions
        for (int si=4;si<_steps.Length;si++) {
            // for each diagonal neighbor
            Vector2Int step = _steps[si];
            Vector2Int neighbor = posGC + step;

            // if it's not on the map or not blocked, then no collision possible
            if (!onMap(neighbor)) continue;
            if (!_map[neighbor.x, neighbor.y].isBlocked) continue;

            // otherwise, convert the neighbor to world position and calculate the distance
            Vector2 neighborPos = gcToVector(neighbor);
            Vector2 penetration = new Vector2(step.x * (pos.x + (extents.x * step.x) - (neighborPos.x - (TILE_SIZE.x/2 * step.x))),
                                      step.y * (pos.y + (extents.y * step.y) - (neighborPos.y - (TILE_SIZE.y/2 * step.y))));

            // in the diagonals, if penetration x AND y are positive, we have collison
            if (penetration.x > 0 && penetration.y > 0) {
                // if you find any one collision, return, you're done
                return new Vector2(-step.x * penetration.x, -step.y * penetration.y);
            }
        }

        // no diagonal collision, check cardinal neighbors (same algorithm)
        for (int si=0;si<4;si++) {
            Vector2Int step = _steps[si];
            Vector2Int neighbor = posGC + step;

            // only need to check if blocked
            if (!onMap(neighbor)) continue;
            if (!_map[neighbor.x, neighbor.y].isBlocked) continue;

            Vector2 neighborPos = gcToVector(neighbor);
            Vector2 penetration = new Vector2(step.x * (pos.x + (extents.x * step.x) - (neighborPos.x - (TILE_SIZE.x/2 * step.x))),
                                      step.y * (pos.y + (extents.y * step.y) - (neighborPos.y - (TILE_SIZE.y/2 * step.y))));

            // in the cardinal directions, if penetration x OR y is positive, we have collision on that axis
            if (penetration.x > 0 || penetration.y > 0) {
                // if you find any one collision, return, you're done
                return new Vector2(-step.x * penetration.x, -step.y * penetration.y);
            }
        }

        // no collison
        return Vector2.zero;
    }

    /*********************************************************************************************
        Loading tile map from text file
     */

    private void readMapFile()
    {
        // file reading made easy
        string[] lines = System.IO.File.ReadAllLines("Assets/map.txt"); // "\\" to "/"

        // use counters to keep track of grid x, y (first line in the file is at the top, so start at biggest y value and count down)
        int grid_x=0, grid_y=HEIGHT-1;

        // for each line in the map file
        foreach (string line in lines)
        {
            // x starts at zero
            grid_x = 0;
            foreach (string sval in line.Split()) 
            {
                // get the next value (0 for open, 1 for a wall)
                int val = int.Parse(sval);

                // instantiate the tile prefab at the current grid x, y
                // GameObject tile = Instantiate(_tilePrefabs[0], new Vector2(0, 0), Quaternion.identity); // *** this is wrong ***
                GameObject tile = Instantiate(_tilePrefabs[val], new Vector2(grid_x, grid_y), Quaternion.identity);
                
                // and parent it to this manager entity
                tile.transform.parent = transform;

                // set the search array blocked value based on the map value at the current grid x, y
                // _map[grid_x,grid_y].isBlocked = false; // *** this is wrong ***
                if(val == 1)
                {
                    _map[grid_x, grid_y].isBlocked = true;
                }
                else
                {
                    _map[grid_x, grid_y].isBlocked = false;
                }

                // set the debugging array values at the current grid x, y

                // the tile spriterenderer for changing color
                _tiles[grid_x,grid_y] = tile.GetComponent<SpriteRenderer>();

                // and the text label to show the path search values
                GameObject label = Instantiate(_labelPrefab, Camera.main.WorldToScreenPoint(tile.transform.position),
                                            Quaternion.identity);
                _labels[grid_x,grid_y] = label.GetComponent<Text>();
                _labels[grid_x,grid_y].text = "";
                label.transform.SetParent(_uiCanvas.transform, true);

                grid_x++;
            }
            grid_y--;
        }
    }

    /*********************************************************************************************
        Pathfinding (A*)
        - getPath is the public API, takes start/end in world space vectors
     */
    public List<Vector2> getPath(Vector2 start, Vector2 end, bool debug=false)
    {
        // call the A* search to do the real work
        List<Vector2Int> path = createPath(vectorToGC(start), vectorToGC(end), debug);

        // no path found
        if (path == null) return null;

        // convert the sequence of grid cells returned by A* into a sequence of world positions
        List<Vector2> vpath = new List<Vector2>();
        foreach (Vector2Int gc in path)
        {
            vpath.Add(gcToVector(gc));
        }
        return vpath;
    }

    /***************************
        Pathfinding helper functions
        - grid coordinates are stored as Vector2Int
    */

    // convert from a world position to the coordinates of the grid cell it's in
    private Vector2Int vectorToGC(Vector2 v)
    {
        return new Vector2Int((int)(v.x+(TILE_SIZE.x/2))/(int)TILE_SIZE.x, (int)(v.y+(TILE_SIZE.y/2))/(int)TILE_SIZE.y);
    }

    // convert from the coordinates of a grid cell to a world position (the center of the cell)
    private Vector2 gcToVector(Vector2Int gc)
    {
        return new Vector2((gc.x+0.0f)*TILE_SIZE.x, (gc.y+0.0f)*TILE_SIZE.y);
    }

    // check it the coordinates specify a grid cell that is within the map boundaries
    private bool onMap(Vector2Int gc)
    {
        return gc.x >= 0 && gc.x < WIDTH && gc.y >= 0 && gc.y < HEIGHT;
    }

    /***************************
        The A* algorithm
    */

    private List<Vector2Int> createPath(Vector2Int start, Vector2Int end, bool debug=false)
    {
        // degenerate cases first

        // end not on map, no path to get there
        if (!onMap(end))
        {
            return null;
        }

        // start is end, you're done
        if (start == end) {
            return null;
        }

        // end cell itself is blocked, no path to get there
        if (_map[end.x, end.y].isBlocked) {
            return null;
        }

        // reset the search array for a new search
        for (int i=0;i<WIDTH;i++) {
            for (int j=0;j<HEIGHT;j++) {
                // clear visited and A* g values
                _map[i,j].visited = false;
                _map[i,j].g = 0;

                // if debugging, reset the tile colors and labels
                if (debug && !_map[i,j].isBlocked)
                {
                    _tiles[i,j].color = TILE_COLOR;
                    _labels[i,j].text = "";
                }
            }
        }

        // start the search by starting the open list with just the start position
        _openList.Clear();
        _openList.Add(new KeyValuePair<float, Vector2Int>(0, start));

        while (_openList.Count != 0) {
            // get the next best cell by sorting the open list by f value
            _openList.Sort(new ByKey());
            KeyValuePair<float, Vector2Int> first = _openList[0];
            _openList.Remove(first);

            Vector2Int next = first.Value;

            // add to "closed list" by marking it visited in the search array
            _map[next.x, next.y].visited = true;

            if (next == end) {
                // done! generate the path list by traversing the parent links
                return extractPath(start, end, debug);
            }

            // add children (loop over steps to generate)
            for (int si=0;si<_steps.Length;si++) {
                Vector2Int child = next + _steps[si];

                // skip this child if it is not on the map
                if (!onMap(child)) continue;

                // skip this child if it is blocked (can't walk on it anyway), or has already been visited
                // *** you add here ***
                if((_map[child.x,child.y].isBlocked == true) || (_map[child.x,child.y].visited == true)) continue;


                // calculate the A* values for this child
                // float g = 0; // *** this is wrong ***
                float g = Mathf.Sqrt(Mathf.Pow(child.x - start.x, 2) + Mathf.Pow(child.y - start.y, 2));   // start to current node
                // float h = 0; // *** also wrong ***
                float h = Mathf.Sqrt(Mathf.Pow(end.x - child.x, 2) + Mathf.Pow(end.y - child.y, 2));   // current to end 
                float f = g + h;                                // start to end node

                // is this child already in the open list?
                KeyValuePair<float, Vector2Int> find = _openList.Find(x => x.Value == child);
                if (find.Key != 0) 
                {
                    // if so, see if the new path to it is shorter than the one already in there
                    if (g < _map[find.Value.x, find.Value.y].g)
                    {
                        // new one is better, get rid of old
                        _openList.Remove(find);
                    } else {
                        // old is better, no need to add this child
                        continue;
                    }
                }

                // add this child to the open list
                _openList.Add(new KeyValuePair<float, Vector2Int>(f, child));
                // and store its search values in the search array for later use
                _map[child.x, child.y].g = g;
                _map[child.x, child.y].h = h;
                _map[child.x, child.y].f = f;
                // store its parent too so we can backtrace the path when we get to the end
                _map[child.x, child.y].parent = next;

                if (debug)
                {
                    // color the child tile being considered and set label to its g value
                    _tiles[child.x, child.y].color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
                    _labels[child.x, child.y].text = g.ToString("f2");
                }
            }
        }
        return null;
    }

    // extract the finished path by backtracing through the parent links
    private List<Vector2Int> extractPath(Vector2Int start, Vector2Int end, bool debug=false)
    {
        List<Vector2Int> path = new List<Vector2Int>();

        // add the ending cell first
        path.Add(end);

        if (debug) {
            _tiles[end.x, end.y].color = new Color(.5f, .5f, 1.0f, 1.0f);
        }

        // then go to the parent cell
        Vector2Int next = _map[end.x, end.y].parent;

        // and keep going until we back to the start cell
        while (next != start) {
            path.Add(next);
            if (debug) {
                _tiles[next.x, next.y].color = new Color(.5f, .5f, 1.0f, 1.0f);
            }

            next = _map[next.x, next.y].parent;
        }

        // reverse the path order so start is first and end is last        
        path.Reverse();
        return path;
    }
}

// used to sort the open list by f
public class ByKey : IComparer<KeyValuePair<float, Vector2Int>>
{
    public int Compare(KeyValuePair<float, Vector2Int> a, KeyValuePair<float, Vector2Int> b) {
        return a.Key.CompareTo(b.Key);
    }
}
