import random
import heapq
from agentpy import Model, AgentList
from agent import RobotAgent

class Environment(Model):
    def setup(self):
        self.width = self.p.width
        self.height = self.p.height
        self.num_agents = self.p.num_agents
        self.num_boxes = self.p.num_boxes
        self.num_shelves = self.p.num_shelves

        self.grid = [[0 for _ in range(self.height)] for _ in range(self.width)]
        self.agents = AgentList(self, self.num_agents, RobotAgent)
        self.boxes = []
        self.shelves = []
        self.logs = []  # Aquí almacenamos los estados

    def init_positions(self):
        """
        Inicializa las posiciones de agentes, cajas y estanterías.
        """
        for agent in self.agents:
            agent.position = self.get_random_empty_position()
            self.grid[agent.position[0]][agent.position[1]] = 1

        for _ in range(self.num_boxes):
            position = self.get_random_empty_position()
            self.boxes.append(position)
            self.grid[position[0]][position[1]] = 2

        for _ in range(self.num_shelves):
            position = self.get_random_empty_position()
            self.shelves.append([position, 0])  # Cada estantería inicia con 0 cajas

    def get_random_empty_position(self):
        """
        Encuentra una posición aleatoria vacía en el grid.
        """
        border_offset = 1
        while True:
            x = random.randint(border_offset, self.width - 1 - border_offset)
            y = random.randint(border_offset, self.height - 1 - border_offset)
            if self.grid[x][y] == 0:
                return (x, y)

    def is_position_free(self, position):
        """
        Verifica si una posición está libre en el grid.
        """
        x, y = position
        return 0 <= x < self.width and 0 <= y < self.height and self.grid[x][y] == 0

    def record_step(self):
        """
        Registra el estado actual del entorno en los logs.
        """
        step_log = {
            "agents": [{"id": agent.id, "position": agent.position, "carrying_box": agent.carrying_box}
                       for agent in self.agents],
            "boxes": [{"position": agent.box_position if agent.carrying_box else box}
                      for agent in self.agents for box in self.boxes],
            "shelves": [{"position": shelf[0], "box_count": shelf[1]} for shelf in self.shelves]
        }
        self.logs.append(step_log)

    def step(self):
        """
        Ejecuta un paso de la simulación.
        """
        for agent in self.agents:
            if not agent.carrying_box:
                agent.deductive_reasoning()
                for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                    adjacent = (agent.position[0] + dx, agent.position[1] + dy)
                    if adjacent in self.boxes:
                        agent.pick_up_box(adjacent, self)
                        break
                else:
                    agent.move_in_direction(self)
            else:
                self.move_agent_to_nearest_available_shelf(agent)
        self.record_step()

    def move_agent_to_nearest_available_shelf(self, agent):
        """
        Mueve al agente hacia la estantería más cercana con espacio disponible.
        """
        target_shelf = self.find_nearest_available_shelf(agent.position)
        if target_shelf:
            path = self.find_path_a_star(agent.position, target_shelf[0])
            if path:
                agent.position = path[0]
                agent.box_position = agent.position
                if self.is_adjacent(agent.position, target_shelf[0]):
                    self.drop_box_on_shelf(agent, target_shelf)

    def is_adjacent(self, position, shelf_position):
        dx = abs(position[0] - shelf_position[0])
        dy = abs(position[1] - shelf_position[1])
        return dx + dy == 1  # Adyacente en Manhattan

    def drop_box_on_shelf(self, agent, shelf):
        """
        Permite al agente dejar una caja en la estantería si hay espacio disponible.
        """
        if shelf[1] < 5:
            shelf[1] += 1
            if agent.box_position in self.boxes:
                self.boxes.remove(agent.box_position)
            agent.carrying_box = False
            agent.box_position = None

    def find_nearest_available_shelf(self, position):
        """
        Encuentra la estantería más cercana que tenga menos de 5 cajas.
        """
        available_shelves = [shelf for shelf in self.shelves if shelf[1] < 5]
        if available_shelves:
            return min(available_shelves, key=lambda shelf: self.heuristic(position, shelf[0]))
        return None

    def find_path_a_star(self, start, goal):
        """
        Implementa el algoritmo A* para encontrar el camino más corto.
        """
        open_set = []
        heapq.heappush(open_set, (0, start))
        came_from = {}
        g_score = {start: 0}
        f_score = {start: self.heuristic(start, goal)}
        
        while open_set:
            _, current = heapq.heappop(open_set)
            if current == goal:
                return self.reconstruct_path(came_from, current)
            for neighbor in self.get_neighbors(current):
                tentative_g_score = g_score[current] + 1
                if neighbor not in g_score or tentative_g_score < g_score[neighbor]:
                    came_from[neighbor] = current
                    g_score[neighbor] = tentative_g_score
                    f_score[neighbor] = tentative_g_score + self.heuristic(neighbor, goal)
                    heapq.heappush(open_set, (f_score[neighbor], neighbor))
        return []

    def reconstruct_path(self, came_from, current):
        """
        Reconstruye el camino desde el nodo objetivo hasta el inicio.
        """
        path = []
        while current in came_from:
            path.insert(0, current)
            current = came_from[current]
        return path

    def get_neighbors(self, position):
        """
        Devuelve los vecinos válidos de una posición.
        """
        x, y = position
        neighbors = [(x, y - 1), (x, y + 1), (x - 1, y), (x + 1, y)]
        return [n for n in neighbors if self.is_position_free(n)]

    def heuristic(self, position, goal):
        """
        Calcula la distancia de Manhattan entre dos posiciones.
        """
        return abs(position[0] - goal[0]) + abs(position[1] - goal[1])
