using System;
using System.Collections;
using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    public struct TasksCollection
    {
        public List<GameTask> Tasks;
    }
    
    public class GameTaskManager : MonoBehaviour 
    {
        public List<TasksCollection> TasksCollection;
        [NonSerialized] public int CurrentTaskIndex = -1;
        public float TasksStartDelay = 0.5f;
        public float StartNextTaskDelay = 0.5f;
        
        //Runtime
        [NonSerialized] public List<GameTask> Tasks;

        public void SetTasks(int tasksIndex)
        {
            Tasks = new List<GameTask>();
            if (TasksCollection.IsNullOrEmpty() || tasksIndex < 0) return;
            if (TasksCollection.IsValidIndex(tasksIndex))
            {
                Tasks = TasksCollection[tasksIndex].Tasks;
            }
            else
            {
                Tasks = TasksCollection[0].Tasks;
            }
            
            SetupTasks();
        }
        
        public void SetTasks(List<GameTask> tasks)
        {
            Tasks = tasks;
            SetupTasks();
        }
        
        public void SetupTasks()
        {
            foreach (var task in Tasks)
            {
                task.SetupTask();
            }
        }
        
        public virtual void StartTasks(int taskToStart=0)
        {
            StartCoroutine(_StartTasks(taskToStart));
        }

        private void Update()
        {
            if (CurrentTaskIndex < 0) return;
            if (!Tasks.IsValidIndex(CurrentTaskIndex)) return;
            if (Tasks[CurrentTaskIndex].Completed) return;
            Tasks[CurrentTaskIndex].UpdateTask(Time.deltaTime);
        }
        private IEnumerator _StartTasks(int taskToStart=0)
        {
            yield return new WaitForSeconds(TasksStartDelay);
            var currentTask = GetCurrentTask();
            if (currentTask != null)
            {
                currentTask.EndTask();
            }

            CurrentTaskIndex = taskToStart;
            foreach (var task in Tasks)
            {
                task.ParentTaskManager = this;
                task.Completed = false;
            }

            if (CurrentTaskIndex >= Tasks.Count) yield break;
            Tasks[CurrentTaskIndex].StartTask();
        }
        
        public virtual void OnTaskCompleted()
        {
            Invoke("SetNextTask", StartNextTaskDelay);
        }
        public virtual void SetNextTask()
        {
            CurrentTaskIndex++;
            if(Tasks.Count <= CurrentTaskIndex) return;
            Tasks[CurrentTaskIndex].StartTask();

        }

        public virtual void Restart()
        {
            GameTask currTask = GetCurrentTask();
            if(currTask != null) currTask.EndTask();
            CurrentTaskIndex = 0;
        }

        public GameTask GetCurrentTask()
        {
            if(CurrentTaskIndex < 0 || CurrentTaskIndex >= Tasks.Count) return null;
            return Tasks[CurrentTaskIndex];
        }
    }
}