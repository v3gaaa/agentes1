import agentpy as ap
import random
import math
from flask import Flask, jsonify, request

app = Flask(__name__)

class WarehouseAgent(ap.Agent):
    def setup(self):
        self.object_count = 0
        self.is_carrying_object = False
        self.current_position = self.model.random_position()
        self.target_position = None

    def step(self):
        if not self.is_carrying_object:
            self.find_closest_object()
        if self.is_carrying_object:
            self.move_towards_target()

    def find_closest_object(self):
        closest_distance = float('inf')
        for obj in self.model.objects:
            distance = self.distance_to(obj)
            if distance < closest_distance:
                closest_distance = distance
                self.target_position = obj['position']
        if self.target_position:
            self.is_carrying_object = True
            print(f"Agent {self.id} found an object at {self.target_position}")

    def move_towards_target(self):
        if self.target_position:
            self.current_position = self.target_position
            self.is_carrying_object = False
            self.object_count += 1
            print(f"Agent {self.id} moved to {self.target_position} and picked up an object")

    def distance_to(self, obj):
        return math.sqrt((self.current_position[0] - obj['position'][0])**2 +
                         (self.current_position[1] - obj['position'][1])**2 +
                         (self.current_position[2] - obj['position'][2])**2)

class WarehouseModel(ap.Model):
    def setup(self):
        self.objects = [{'id': i, 'position': [random.randint(0, 10), random.randint(0, 10), 0], 'count': 0} for i in range(10)]
        self.agents = ap.AgentList(self, 5, WarehouseAgent)

    def step(self):
        self.agents.step()

@app.route('/setup', methods=['GET'])
def setup():
    global model
    model = WarehouseModel()
    model.setup()
    return jsonify({'agents': 5, 'objects': len(model.objects)})

@app.route('/step', methods=['POST'])
def step():
    model.step()
    return jsonify({'agents': [agent.object_count for agent in model.agents],
                    'objects': model.objects})

@app.route('/next_action', methods=['GET'])
def next_action():
    # Determine the next action for the robot
    # This is a simplified example, you can implement more complex logic
    actions = ["move_forward", "rotate", "stop", "resume", "turn"]
    action = random.choice(actions)
    return jsonify(action)

@app.route('/robot/pickup', methods=['POST'])
def robot_pickup():
    data = request.json
    robot_id = data['robot_id']
    print(f"Robot {robot_id} picks up object")
    return jsonify({"status": "success", "action": "pickup"})

@app.route('/robot/drop', methods = ['POST'])
def robot_drop():
    data = request.json
    robot_id = data['robot_id']
    print(f"Robot {robot_id} drops object")
    return jsonify({"status": "success", "action": "drop"})

if __name__ == '__main__':
    app.run(debug=True)