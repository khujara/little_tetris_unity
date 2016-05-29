using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

struct tetris_mesh
{
	// NOTE(flo): WHY ON EARTH should this be private by default ???
	public Vector3[] pos;
	public Vector2[] uvs;
	public int[] indices;
	public Color32[] colors;
};

struct tetromino_type
{
	public uint color;
	public uint[] rot;
};

struct tetromino
{
	public int pos_x;
	public int pos_y;
	public int rotation;
	public int type;
};

struct grid_case
{
	public uint color;
	public bool is_cur;
};

struct grid
{
	public grid_case[] gc;
};

public class khjr_tetris : MonoBehaviour 
{
	enum TetrominoType
	{
		I = 1,
		J = 2,
		L = 3,
		O = 4,
		S = 5,
		T = 6,
		Z = 7,
	};

	enum TetrominoRot
	{
		Rot0 = 0,
		Rot90 = 1,
		Rot180 = 2,
		Rot270 = 3,
	}

	static uint COL_COUNT = 10;
	static uint ROW_COUNT = 20;
	static uint LINES_PER_LEVEL = 10;

	grid gr;
	tetromino_type[] tetrominoes;
	tetromino cur;
	int next_type;

	float t;
	float base_speed;
	float speed;

	bool death;

	// NOTE(flo): score
	float score;
	int line_til_next_lvl;
	int level;
	int total_lines;

	// NOTE(flo): display
	tetris_mesh t_mesh;
	tetris_mesh next_t_mesh;
	Mesh mesh;
	Mesh next_mesh;
	Color32 next_base_color;

	Text score_txt;
	Text line_txt;
	Text level_txt;

	Transform tr;


	Color32 argb_to_unity_color32(uint argb)
	{
		Color32 res;
		res.a = (byte)(argb >> 24);;
		res.r = (byte)((argb >> 16) & 0xFF); 
		res.g = (byte)((argb >> 8) & 0xFF);
		res.b = (byte)(argb & 0xFF);
		return(res);
	}

	int get_index_from_tetromino_type(TetrominoType type)
	{
		int res = (int)type - 1;
		return(res);
	}

	void compute_mesh(tetris_mesh m, int row_c, int col_c, float base_y, float base_x, float base_z, 
		float scale, Color32 color)
	{
		float pos_y = base_y;
		float pos_z = base_z;
		for(int y = 0; y < row_c; ++y, pos_y += scale)
		{
			float pos_x = base_x;
			int row = (int)(y * col_c);
			for(int x = 0; x < col_c; ++x, pos_x += scale)
			{
				int ind_0 = (x + row) * 4;
				int ind_1 = (x + row) * 6;

				m.pos[ind_0 + 0] = new Vector3(pos_x, pos_y, pos_z);
				m.pos[ind_0 + 1] = new Vector3(pos_x, pos_y + scale, pos_z);
				m.pos[ind_0 + 2] = new Vector3(pos_x + scale, pos_y, pos_z);
				m.pos[ind_0 + 3] = new Vector3(pos_x + scale, pos_y + scale, pos_z);

				m.indices[ind_1 + 0] = ind_0 + 0;
				m.indices[ind_1 + 1] = ind_0 + 1;
				m.indices[ind_1 + 2] = ind_0 + 2;
				m.indices[ind_1 + 3] = ind_0 + 1;
				m.indices[ind_1 + 4] = ind_0 + 3;
				m.indices[ind_1 + 5] = ind_0 + 2;

				m.colors[ind_0 + 0] = color;
				m.colors[ind_0 + 1] = color;
				m.colors[ind_0 + 2] = color;
				m.colors[ind_0 + 3] = color;

		  		m.uvs[ind_0 + 0] = new Vector2 (0, 0);
		  		m.uvs[ind_0 + 1] = new Vector2 (0, 1);
		  		m.uvs[ind_0 + 2] = new Vector2 (1, 0);
		  		m.uvs[ind_0 + 3] = new Vector2 (1, 1);
			}

		}
	}

	void send_tetris_mesh_to_mesh(tetris_mesh src, Mesh dst)
	{
		dst.Clear();
		dst.vertices = src.pos;
		dst.triangles = src.indices;
		dst.uv = src.uvs;
		dst.colors32 = src.colors;
		dst.Optimize();
		dst.RecalculateNormals();
	}

	tetromino_type set_tetromino_type(uint c, uint r_0, uint r_90, uint r_180, uint r_270)
	{
		tetromino_type res;
		res.rot = new uint[4];
		res.color = c;
		res.rot[(int)TetrominoRot.Rot0] = r_0;
		res.rot[(int)TetrominoRot.Rot90] = r_90;
		res.rot[(int)TetrominoRot.Rot180] = r_180;
		res.rot[(int)TetrominoRot.Rot270] = r_270;
		return(res);
	}


	tetromino next_tetromino()
	{
		for(int i = 0; i < this.gr.gc.Length; ++i)
		{
			this.gr.gc[i].is_cur = false;
		}
		tetromino res;
		res.pos_x = 3;
		res.pos_y = (int)ROW_COUNT - 2;
		res.rotation = 0;
		res.type = this.next_type;
		this.next_type = Random.Range(0, 7);
		int row = 0;
		for(int y = 0; y < 4; ++y)
		{
			for(int x = 0; x < 4; ++x)
			{
				int index = (x + row) * 4;
				int mask = 0x8000 >> (x + row);
				tetromino_type type = this.tetrominoes[this.next_type];
				Color32 color = ((type.rot[0] & mask) > 0) ? argb_to_unity_color32(type.color) : this.next_base_color;
				this.next_t_mesh.colors[index + 0] = color;
				this.next_t_mesh.colors[index + 1] = color;
				this.next_t_mesh.colors[index + 2] = color;
				this.next_t_mesh.colors[index + 3] = color;
			}
			row += 4;
		}
		send_tetris_mesh_to_mesh(this.next_t_mesh, this.next_mesh);
		move(res, res.pos_x, res.pos_y);
		return(res);
	}


	void rotate(tetromino te, int new_rot)
	{
		int row = 0;
		int col = 0;
		tetromino_type type = this.tetrominoes[te.type];
		uint rot = type.rot[te.rotation];
		uint desired_rot = type.rot[new_rot];
		for(uint b = 0x8000; b > 0; b = b >> 1)
		{
			int index = (int)((te.pos_x + col) + ((te.pos_y + row) * COL_COUNT));
			if(index >= 0 && index < this.gr.gc.Length - 1)
			{
				if((rot & b) > 0)
				{
					this.gr.gc[index].color = 0xFFFFFFFF;
					this.gr.gc[index].is_cur = false;
				}
				if((desired_rot & b) > 0)
				{
					this.gr.gc[index].color = type.color;
					this.gr.gc[index].is_cur = true;
				}
			}
			if(++col == 4)
			{
				++row;
				col = 0;
			}
		}
	}

	void move(tetromino te, int new_x, int new_y)
	{
		int row = 0;
		int col = 0;
		tetromino_type type = this.tetrominoes[te.type];
		for(uint b = 0x8000; b > 0; b = b >> 1)
		{
			if((type.rot[te.rotation] & b) > 0)
			{
				int old_index = (int)((te.pos_x + col) + ((te.pos_y + row) * COL_COUNT));
				int new_index = (int)((new_x + col) + ((new_y + row) * COL_COUNT));
				if(old_index >= 0 && old_index < this.gr.gc.Length - 1)
				{
					this.gr.gc[old_index].color = 0xFFFFFFFF;
					this.gr.gc[old_index].is_cur = false;
				}
				if(new_index >= 0 && new_index < this.gr.gc.Length - 1)
				{
					this.gr.gc[new_index].color = type.color;
					this.gr.gc[new_index].is_cur = true;
				}
			}

			if(++col == 4)
			{
				++row;
				col = 0;
			}
		}
	}

	bool collide(tetromino te, int new_rot, int new_x, int new_y)
	{
		bool res = false;
		int row = 0;
		int col = 0;
		int b_i = 0;
		tetromino_type type = this.tetrominoes[te.type];
		uint rot = type.rot[new_rot];
		for(uint b = 0x8000; b > 0; b = b >> 1, ++b_i)
		{
			int index = (int)((new_x + col) + ((new_y + row))  * COL_COUNT);
			bool collide = false;
			if(index < (this.gr.gc.Length - 1) && index >= 0)
			{
				grid_case gc = this.gr.gc[index]; 
				collide = (gc.color != 0xFFFFFFFF) && (!gc.is_cur);
			}
			bool border = ((new_y + row) < 0) || ((new_x + col) < 0) || ((new_x + col) >= COL_COUNT);
			if(((rot & b) > 0) && (border || collide))
			{
				res = true;
				break;
			}
			if(++col == 4)
			{
				++row;
				col = 0;
			}
		}

		return(res);
	}

	void remove_lines()
	{
		int[] lines = new int[10];
		int to_remove = 0;
		int total_removed_lines = 0;
		for(int y = 0; y < ROW_COUNT; ++y)
		{
			bool is_full = true;
			for(int x = 0; x < COL_COUNT; ++x)
			{
				int index = (int)(x + y * COL_COUNT);
				if(this.gr.gc[index].color == 0xFFFFFFFF)
				{
					is_full = false;
					break;
				}
			}
			if(is_full)
			{
				int new_remove = to_remove;
				lines[to_remove++] = y - new_remove;
			}
		}

		int descend = 0;
		for(int i = 0; i < to_remove; ++i)
		{
			++descend;

			int begin_y = lines[i];
			for(int y = begin_y; y < ROW_COUNT - 1; ++y)
			{
				for(int x = 0; x < COL_COUNT; ++x)
				{
					int dst = (int)(x + (y * COL_COUNT));
					int src = (int)(x + ((y + 1) * COL_COUNT));
					this.gr.gc[dst] = this.gr.gc[src];
				}

			}
		}

		total_removed_lines += to_remove;
		this.line_til_next_lvl += total_removed_lines;
		this.total_lines += total_removed_lines;

		if(total_removed_lines == 1)
		{	
			this.score += 40 * (this.level + 1); 
		}
		else if(total_removed_lines == 2)
		{
			this.score += 100 * (this.level + 1);
		}
		else if(total_removed_lines == 3)
		{
			this.score += 300 * (this.level + 1);
		}
		else if(total_removed_lines >= 4)
		{
			this.score += 1220 * (this.level + 1);
		}
		if(this.line_til_next_lvl >= LINES_PER_LEVEL)
		{
			this.level++;
			// TODO(flo): this value is here for lack of better ones...
			this.base_speed *= 0.8f;
			this.line_til_next_lvl = 0;
		}
	}

	void Start()
	{
		this.tr = transform;
		this.mesh = GetComponent<MeshFilter>().mesh;
		this.score_txt = tr.GetChild(0).GetChild(1).GetComponent<Text>();
		this.level_txt = tr.GetChild(0).GetChild(3).GetComponent<Text>();
		this.line_txt = tr.GetChild(0).GetChild(5).GetComponent<Text>();
		this.next_mesh = tr.GetChild(1).GetComponent<MeshFilter>().mesh;
		uint total_grid_count = COL_COUNT * ROW_COUNT;

		this.score = 0;
		this.level = 0;
		this.line_til_next_lvl = 0;
		this.total_lines = 0;
		this.base_speed = 0.3f;
		this.speed = this.base_speed;

		this.death = false;

		this.t_mesh.pos = new Vector3[4 * total_grid_count];
		this.t_mesh.uvs = new Vector2[4 * total_grid_count];
		this.t_mesh.indices = new int[6 * total_grid_count];
		this.t_mesh.colors = new Color32[4 * total_grid_count];

		this.next_t_mesh.pos = new Vector3[4 * total_grid_count];
		this.next_t_mesh.uvs = new Vector2[4 * total_grid_count];
		this.next_t_mesh.indices = new int[6 * total_grid_count];
		this.next_t_mesh.colors = new Color32[4 * total_grid_count];
		this.next_base_color = new Color32(0x00, 0x00, 0x00, 0xFF);

		this.gr.gc = new grid_case[total_grid_count];
		for(int i = 0; i < gr.gc.Length; ++i)
		{
			this.gr.gc[i].color = 0xFFFFFFFF;
		}

		Vector3 bottom_left = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, 0));
		Vector3 top_right = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));
		Vector3 size = top_right - bottom_left;

		float scale = size.y / ROW_COUNT;
		float w = COL_COUNT * scale;
		float base_x =  bottom_left.x + size.x * 0.5f - w * 0.5f;
		Color32 color = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

		compute_mesh(this.t_mesh, (int)ROW_COUNT, (int)COL_COUNT, bottom_left.y, base_x, tr.position.x, scale, color);
		send_tetris_mesh_to_mesh(this.t_mesh, this.mesh);
		
		compute_mesh(this.next_t_mesh, 4, 4, top_right.y - 5 * scale, base_x + w + scale, 0, scale, this.next_base_color);

		this.tetrominoes = new tetromino_type[7];
		this.tetrominoes[(int)get_index_from_tetromino_type(TetrominoType.I)] = 
			set_tetromino_type(0xFF00FFFF, 0x0F00, 0x2222, 0x00F0, 0x4444);
		this.tetrominoes[(int)get_index_from_tetromino_type(TetrominoType.J)] = 
			set_tetromino_type(0xFF0000FF, 0x44C0, 0x8E00, 0x6440, 0x0E20);
		this.tetrominoes[(int)get_index_from_tetromino_type(TetrominoType.L)] = 
			set_tetromino_type(0xFFFFA500, 0x4460, 0x0E80, 0xC440, 0x2E00);
		this.tetrominoes[(int)get_index_from_tetromino_type(TetrominoType.O)] = 
			set_tetromino_type(0xFFFFFF00, 0xCC00, 0xCC00, 0xCC00, 0xCC00);
		this.tetrominoes[(int)get_index_from_tetromino_type(TetrominoType.S)] = 
			set_tetromino_type(0xFF00FF00, 0x06C0, 0x8C40, 0x6C00, 0x4620);
		this.tetrominoes[(int)get_index_from_tetromino_type(TetrominoType.T)] = 
			set_tetromino_type(0xFF800080, 0x0E40, 0x4C40, 0x4E00, 0x4640);
		this.tetrominoes[(int)get_index_from_tetromino_type(TetrominoType.Z)] = 
			set_tetromino_type(0xFFFF0000, 0x0C60, 0x4C80, 0xC600, 0x2640);

		this.next_type = Random.Range(0, 7);
		this.cur = next_tetromino();

	}

	void Update()
	{

		if(this.death)
		{
			if(Input.GetKeyDown(KeyCode.S))
			{
				SceneManager.LoadScene(0);
			}
			return;
		}

		this.t += Time.deltaTime;
		int new_x = this.cur.pos_x;
		int new_y = this.cur.pos_y;
		bool next = false;
		int dir_x = 0;
		int new_rot = this.cur.rotation;
		int desired_rot = new_rot;
		
		int move_y = this.cur.pos_y;

		if(this.t >= this.speed)
		{
			move_y -= 1;
			bool col = collide(this.cur, this.cur.rotation, this.cur.pos_x, move_y); 
			if(col)
			{
				// TODO(flo): not quite correct but okay for our purposes?
				if(move_y >= ROW_COUNT - 3)
				{
					this.death = true;
				}
				next = true;
			}
			else
			{
				new_y = move_y;
			}
			this.t = 0.0f;
		}

		if(Input.GetKeyDown(KeyCode.LeftArrow))
		{
			dir_x = -1;
		}
		else if(Input.GetKeyDown(KeyCode.RightArrow))
		{
			dir_x = 1;
		}
		else if(Input.GetKeyDown(KeyCode.DownArrow))
		{
			this.speed = this.base_speed * 0.1f;
		}
		else if(Input.GetKeyUp(KeyCode.DownArrow))
		{
			this.speed = this.base_speed;
		}
		else if(Input.GetKeyDown(KeyCode.Q))
		{
			desired_rot--;
			if(desired_rot < 0)
			{
				desired_rot = 3;
			}
		}
		else if(Input.GetKeyDown(KeyCode.W))
		{
			desired_rot++;
			if(desired_rot > 3)
			{
				desired_rot = 0;
			}
		}
		else if(Input.GetKeyDown(KeyCode.D))
		{
			int test_y = cur.pos_y;
			while(!collide(this.cur, this.cur.rotation, this.cur.pos_x, test_y))
			{
				test_y--;
			}
			new_y = test_y + 1;
		}
		else if(Input.GetKeyDown(KeyCode.S))
		{
			SceneManager.LoadScene(0);
		}

		if(dir_x != 0)
		{
			int test_x = cur.pos_x + dir_x;
			if(!collide(this.cur, this.cur.rotation, test_x, move_y))
			{
				new_x = test_x;
				new_y = move_y;
				next = false;
			}
		}
		move(this.cur, new_x, new_y);
		this.cur.pos_x = new_x;
		this.cur.pos_y = new_y;


		if(desired_rot != new_rot)
		{
			if(!collide(this.cur, desired_rot, this.cur.pos_x, this.cur.pos_y))
			{
				new_rot = desired_rot;
			}
		}

		rotate(this.cur, new_rot);
		this.cur.rotation = new_rot;

		if(next)
		{
			remove_lines();
			this.cur = next_tetromino();
		}


		for(int y = 0; y < ROW_COUNT; ++y)
		{
			for(int x = 0; x < COL_COUNT; ++x)
			{

				int index = (int)(x + y * COL_COUNT);
				grid_case gc = this.gr.gc[index];
				Color32 c = argb_to_unity_color32(gc.color);
				int color_ind = index * 4;
				this.t_mesh.colors[color_ind + 0] = c; 
				this.t_mesh.colors[color_ind + 1] = c; 
				this.t_mesh.colors[color_ind + 2] = c; 
				this.t_mesh.colors[color_ind + 3] = c; 
			}
		}
		send_tetris_mesh_to_mesh(this.t_mesh, this.mesh);

		this.score_txt.text = this.score.ToString();
		this.line_txt.text = this.total_lines.ToString();
		this.level_txt.text = this.level.ToString();
	}
}