import random
from agentpy import Agent

class Ontology:
    def __init__(self):
        self.entities = {}
        self.actions = []
        self.relationships = {}

    def add_entity(self, name, description):
        self.entities[name] = description

    def add_action(self, action):
        self.actions.append(action)

    def add_relationship(self, entity1, relationship, entity2):
        if entity1 not in self.relationships:
            self.relationships[entity1] = []
        self.relationships[entity1].append((relationship, entity2))

    def get_relationship(self, entity):
        return self.relationships.get(entity, [])

    def get_entity(self, name):
        return self.entities.get(name, None)

class RobotAgent(Agent):
    def setup(self):
        self.carrying_box = False
        self.box_position = None
        self.movements = 0
        self.current_direction = None  # Dirección actual del agente
        self.steps_in_current_direction = 0  # Contador de pasos en la dirección actual
        self.known_boxes = []  # Lista de posiciones conocidas de cajas
        self.ontology = Ontology()

    def choose_random_direction(self):
        directions = [(0, 1), (0, -1), (1, 0), (-1, 0)]
        self.current_direction = random.choice(directions)
        self.steps_in_current_direction = 0  # Reiniciar el contador de pasos

    def move_in_direction(self, environment):
        if self.current_direction is None or self.steps_in_current_direction >= 5:
            self.choose_random_direction()

        dx, dy = self.current_direction
        new_position = (self.position[0] + dx, self.position[1] + dy)

        if environment.is_position_free(new_position):
            self.position = new_position
            self.movements += 1
            self.steps_in_current_direction += 1
            return new_position
        else:
            self.choose_random_direction()
            return None

    def pick_up_box(self, box_position, environment):
        if box_position in environment.boxes:
            environment.boxes.remove(box_position)
            self.carrying_box = True
            self.box_position = box_position
            self.ontology.add_relationship(self, 'is_carrying', box_position)
            if box_position in self.known_boxes:
                self.known_boxes.remove(box_position)
            self.ontology.add_entity("box_" + str(box_position), "Picked up box at position {}".format(box_position))

    def deductive_reasoning(self):
        """
        Si el agente tiene conocimiento de una caja en known_boxes intenta ir hacia allá.
        """
        if self.known_boxes:
            target_box = self.known_boxes.pop(0)  
            
            self.ontology.add_relationship(self, 'knows_position', target_box)

            self.current_direction = (
                target_box[0] - self.position[0],
                target_box[1] - self.position[1]
            )
            if abs(self.current_direction[0]) > abs(self.current_direction[1]):
                self.current_direction = (1 if self.current_direction[0] > 0 else -1, 0)
            else:
                self.current_direction = (0, 1 if self.current_direction[1] > 0 else -1)
            self.steps_in_current_direction = 0