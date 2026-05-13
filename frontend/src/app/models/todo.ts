export interface SubTask {
  id: string;
  title: string;
  isComplete: boolean;
}

export interface Todo {
  id: string;
  title: string;
  createdAt: string;
  dueAt: string | null;
  notes: string | null;
  isComplete: boolean;
  subTasks: SubTask[];
}

export interface TodoUpdate {
  title: string;
  dueAt: string | null;
  notes: string | null;
  isComplete: boolean;
}
