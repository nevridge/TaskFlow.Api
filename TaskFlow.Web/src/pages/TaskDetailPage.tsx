import { useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { useTaskQuery, useUpdateTaskMutation, useDeleteTaskMutation } from '@/hooks/useTasks'
import { useNotesQuery, useCreateNoteMutation, useUpdateNoteMutation, useDeleteNoteMutation } from '@/hooks/useNotes'
import { TaskForm } from '@/components/TaskForm'
import { NoteCard } from '@/components/NoteCard'
import { NoteForm } from '@/components/NoteForm'
import { formatDate } from '@/lib/utils'
import type { TaskItemResponseDto, NoteResponseDto, CreateTaskItemDto } from '@/api/client/types.gen'

export function TaskDetailPage() {
  const { id } = useParams<{ id: string }>()
  const taskId = Number(id)

  const { data: taskData, isLoading: taskLoading, error: taskError } = useTaskQuery(taskId)
  const { data: notesData, isLoading: notesLoading } = useNotesQuery(taskId)
  const updateTask = useUpdateTaskMutation()
  const deleteTask = useDeleteTaskMutation()
  const createNote = useCreateNoteMutation(taskId)
  const updateNote = useUpdateNoteMutation(taskId)
  const deleteNote = useDeleteNoteMutation(taskId)

  const [editingTask, setEditingTask] = useState(false)
  const [showNoteForm, setShowNoteForm] = useState(false)
  const [editingNote, setEditingNote] = useState<NoteResponseDto | null>(null)

  const task = taskData?.data as TaskItemResponseDto | undefined
  const notes: NoteResponseDto[] = (notesData?.data as NoteResponseDto[] | undefined) ?? []

  if (taskLoading) return <div className="p-8 text-slate-500">Loading…</div>
  if (taskError || !task) return <div className="p-8 text-red-600">Task not found.</div>

  function handleUpdateTask(data: CreateTaskItemDto) {
    updateTask.mutate({ id: taskId, data }, { onSuccess: () => setEditingTask(false) })
  }

  function handleDeleteTask() {
    if (window.confirm(`Delete "${task!.title}"?`)) {
      deleteTask.mutate(taskId)
    }
  }

  function handleDeleteNote(note: NoteResponseDto) {
    if (window.confirm('Delete this note?')) {
      deleteNote.mutate(Number(note.id))
    }
  }

  return (
    <div className="mx-auto max-w-3xl p-6">
      <nav className="mb-4">
        <Link to="/" className="text-sm text-blue-600 hover:underline">← Back to Tasks</Link>
      </nav>

      {editingTask ? (
        <div className="rounded-lg border bg-white p-6 shadow-sm mb-6">
          <h2 className="text-lg font-semibold mb-4">Edit Task</h2>
          <TaskForm task={task} onSubmit={handleUpdateTask} onCancel={() => setEditingTask(false)} />
        </div>
      ) : (
        <div className="rounded-lg border bg-white p-6 shadow-sm mb-6">
          <div className="flex items-start justify-between gap-4">
            <h1 className="text-2xl font-bold text-slate-900">{task.title}</h1>
            <div className="flex gap-2 shrink-0">
              <button
                onClick={() => setEditingTask(true)}
                className="rounded px-3 py-1.5 text-sm text-slate-600 hover:bg-slate-100 border"
              >
                Edit
              </button>
              <button
                onClick={handleDeleteTask}
                className="rounded px-3 py-1.5 text-sm text-red-600 hover:bg-red-50 border border-red-200"
              >
                Delete
              </button>
            </div>
          </div>
          {task.description && (
            <p className="mt-3 text-slate-600">{task.description}</p>
          )}
          <div className="mt-4 flex flex-wrap gap-4 text-sm text-slate-500">
            <span>Status: <strong className="text-slate-700">{task.status}</strong></span>
            <span>Priority: <strong className="text-slate-700">{task.priority}</strong></span>
            {task.dueDate && <span>Due: <strong className="text-slate-700">{formatDate(task.dueDate)}</strong></span>}
          </div>
        </div>
      )}

      <div>
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-lg font-semibold text-slate-800">Notes</h2>
          {!showNoteForm && !editingNote && (
            <button
              onClick={() => setShowNoteForm(true)}
              className="rounded bg-blue-600 px-3 py-1.5 text-sm text-white hover:bg-blue-700"
            >
              Add Note
            </button>
          )}
        </div>

        {showNoteForm && (
          <div className="rounded-lg border bg-white p-4 shadow-sm mb-3">
            <NoteForm
              onSubmit={data => createNote.mutate(data, { onSuccess: () => setShowNoteForm(false) })}
              onCancel={() => setShowNoteForm(false)}
            />
          </div>
        )}

        {notesLoading ? (
          <p className="text-sm text-slate-400">Loading notes…</p>
        ) : notes.length === 0 ? (
          <p className="text-sm text-slate-400">No notes yet.</p>
        ) : (
          <div className="flex flex-col gap-2">
            {notes.map(note =>
              editingNote?.id === note.id ? (
                <div key={String(note.id)} className="rounded-lg border bg-white p-4 shadow-sm">
                  <NoteForm
                    note={note}
                    onSubmit={data => updateNote.mutate(
                      { id: Number(note.id), data },
                      { onSuccess: () => setEditingNote(null) }
                    )}
                    onCancel={() => setEditingNote(null)}
                  />
                </div>
              ) : (
                <NoteCard
                  key={String(note.id)}
                  note={note}
                  onEdit={setEditingNote}
                  onDelete={handleDeleteNote}
                />
              )
            )}
          </div>
        )}
      </div>
    </div>
  )
}
