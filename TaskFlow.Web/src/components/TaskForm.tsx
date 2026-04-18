import { type FormEvent, useState } from 'react'
import type { TaskItemResponseDto, CreateTaskItemDto } from '@/api/client/types.gen'

interface Props {
  task?: TaskItemResponseDto
  onSubmit: (data: CreateTaskItemDto) => void
  onCancel: () => void
}

export function TaskForm({ task, onSubmit, onCancel }: Props) {
  const [title, setTitle] = useState(task?.title ?? '')
  const [description, setDescription] = useState(task?.description ?? '')
  const [status, setStatus] = useState<string>(task?.status ?? 'draft')
  const [priority, setPriority] = useState<string>(task?.priority ?? 'low')
  const [dueDate, setDueDate] = useState(task?.dueDate ? task.dueDate.split('T')[0] : '')

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    onSubmit({
      title,
      description: description || null,
      status: status as 'draft' | 'todo' | 'completed',
      priority: priority as 'low' | 'medium' | 'high',
      dueDate: dueDate ? `${dueDate}T00:00:00.000Z` : null,
    })
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <label htmlFor="title" className="text-sm font-medium text-slate-700">Title</label>
        <input
          id="title"
          value={title}
          onChange={e => setTitle(e.target.value)}
          required
          className="rounded border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
        />
      </div>
      <div className="flex flex-col gap-1">
        <label htmlFor="description" className="text-sm font-medium text-slate-700">Description</label>
        <textarea
          id="description"
          value={description ?? ''}
          onChange={e => setDescription(e.target.value)}
          rows={3}
          className="rounded border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
        />
      </div>
      <div className="flex gap-4">
        <div className="flex flex-col gap-1 flex-1">
          <label htmlFor="status" className="text-sm font-medium text-slate-700">Status</label>
          <select
            id="status"
            value={status}
            onChange={e => setStatus(e.target.value)}
            className="rounded border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
          >
            <option value="draft">Draft</option>
            <option value="todo">Todo</option>
            <option value="completed">Completed</option>
          </select>
        </div>
        <div className="flex flex-col gap-1 flex-1">
          <label htmlFor="priority" className="text-sm font-medium text-slate-700">Priority</label>
          <select
            id="priority"
            value={priority}
            onChange={e => setPriority(e.target.value)}
            className="rounded border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
          >
            <option value="low">Low</option>
            <option value="medium">Medium</option>
            <option value="high">High</option>
          </select>
        </div>
      </div>
      <div className="flex flex-col gap-1">
        <label htmlFor="dueDate" className="text-sm font-medium text-slate-700">Due Date</label>
        <input
          id="dueDate"
          type="date"
          value={dueDate}
          onChange={e => setDueDate(e.target.value)}
          className="rounded border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
        />
      </div>
      <div className="flex justify-end gap-2">
        <button
          type="button"
          onClick={onCancel}
          className="rounded px-4 py-2 text-sm text-slate-600 hover:bg-slate-100"
        >
          Cancel
        </button>
        <button
          type="submit"
          className="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
        >
          Save
        </button>
      </div>
    </form>
  )
}
