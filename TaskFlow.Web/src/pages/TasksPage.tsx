import { useState } from 'react'
import { useTasksQuery, useCreateTaskMutation, useUpdateTaskMutation, useDeleteTaskMutation } from '@/hooks/useTasks'
import { TaskCard } from '@/components/TaskCard'
import { TaskForm } from '@/components/TaskForm'
import type { TaskItemResponseDto, CreateTaskItemDto } from '@/api/client/types.gen'

type StatusFilter = 'all' | 'draft' | 'todo' | 'completed'
type PriorityFilter = 'all' | 'low' | 'medium' | 'high'
type SortKey = 'title' | 'dueDate' | 'priority'

const priorityOrder = { high: 0, medium: 1, low: 2 }

export function TasksPage() {
  const { data, isLoading, error } = useTasksQuery()
  const createMutation = useCreateTaskMutation()
  const updateMutation = useUpdateTaskMutation()
  const deleteMutation = useDeleteTaskMutation()

  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all')
  const [priorityFilter, setPriorityFilter] = useState<PriorityFilter>('all')
  const [sortKey, setSortKey] = useState<SortKey>('title')
  const [editingTask, setEditingTask] = useState<TaskItemResponseDto | null>(null)
  const [showCreate, setShowCreate] = useState(false)

  const tasks: TaskItemResponseDto[] = (data?.data as TaskItemResponseDto[] | undefined) ?? []

  const filtered = tasks
    .filter(t => statusFilter === 'all' || (t.status ?? '').toLowerCase() === statusFilter)
    .filter(t => priorityFilter === 'all' || (t.priority ?? '').toLowerCase() === priorityFilter)
    .sort((a, b) => {
      if (sortKey === 'title') return (a.title ?? '').localeCompare(b.title ?? '')
      if (sortKey === 'dueDate') return (a.dueDate ?? '').localeCompare(b.dueDate ?? '')
      if (sortKey === 'priority') {
        const pa = priorityOrder[(a.priority ?? '').toLowerCase() as keyof typeof priorityOrder] ?? 2
        const pb = priorityOrder[(b.priority ?? '').toLowerCase() as keyof typeof priorityOrder] ?? 2
        return pa - pb
      }
      return 0
    })

  function handleCreate(data: CreateTaskItemDto) {
    createMutation.mutate(data, { onSuccess: () => setShowCreate(false) })
  }

  function handleUpdate(data: CreateTaskItemDto) {
    if (!editingTask?.id) return
    updateMutation.mutate(
      { id: Number(editingTask.id), data },
      { onSuccess: () => setEditingTask(null) }
    )
  }

  function handleDelete(task: TaskItemResponseDto) {
    if (window.confirm(`Delete "${task.title}"?`)) {
      deleteMutation.mutate(Number(task.id))
    }
  }

  if (isLoading) return <div className="p-8 text-slate-500">Loading tasks…</div>
  if (error) return <div className="p-8 text-red-600">Failed to load tasks.</div>

  return (
    <div className="mx-auto max-w-4xl p-6">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-slate-900">Tasks</h1>
        <button
          onClick={() => setShowCreate(true)}
          className="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
        >
          New Task
        </button>
      </div>

      {(showCreate || editingTask) && (
        <div className="mb-6 rounded-lg border bg-white p-6 shadow-sm">
          <h2 className="text-lg font-semibold mb-4">{editingTask ? 'Edit Task' : 'New Task'}</h2>
          <TaskForm
            task={editingTask ?? undefined}
            onSubmit={editingTask ? handleUpdate : handleCreate}
            onCancel={() => { setShowCreate(false); setEditingTask(null) }}
          />
        </div>
      )}

      <div className="flex flex-wrap gap-3 mb-4">
        <div className="flex gap-2 items-center">
          <label htmlFor="status-filter" className="text-xs text-slate-500 font-medium uppercase">Status</label>
          <select
            id="status-filter"
            value={statusFilter}
            onChange={e => setStatusFilter(e.target.value as StatusFilter)}
            className="rounded border px-2 py-1 text-sm"
          >
            <option value="all">All</option>
            <option value="draft">Draft</option>
            <option value="todo">Todo</option>
            <option value="completed">Completed</option>
          </select>
        </div>
        <div className="flex gap-2 items-center">
          <label htmlFor="priority-filter" className="text-xs text-slate-500 font-medium uppercase">Priority</label>
          <select
            id="priority-filter"
            value={priorityFilter}
            onChange={e => setPriorityFilter(e.target.value as PriorityFilter)}
            className="rounded border px-2 py-1 text-sm"
          >
            <option value="all">All</option>
            <option value="low">Low</option>
            <option value="medium">Medium</option>
            <option value="high">High</option>
          </select>
        </div>
        <div className="flex gap-2 items-center">
          <label htmlFor="sort-key" className="text-xs text-slate-500 font-medium uppercase">Sort</label>
          <select
            id="sort-key"
            value={sortKey}
            onChange={e => setSortKey(e.target.value as SortKey)}
            className="rounded border px-2 py-1 text-sm"
          >
            <option value="title">Title</option>
            <option value="dueDate">Due Date</option>
            <option value="priority">Priority</option>
          </select>
        </div>
      </div>

      {filtered.length === 0 ? (
        <p className="text-slate-500 text-sm">No tasks match your filters.</p>
      ) : (
        <div className="grid gap-3 sm:grid-cols-2">
          {filtered.map(task => (
            <TaskCard
              key={String(task.id)}
              task={task}
              onEdit={setEditingTask}
              onDelete={handleDelete}
            />
          ))}
        </div>
      )}
    </div>
  )
}
