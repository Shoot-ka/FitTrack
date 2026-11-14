const find = (id) => document.getElementById(id);
const form = find("login-form");
const number = new Intl.NumberFormat(undefined, { maximumFractionDigits: 2 });
const shortDate = new Intl.DateTimeFormat(undefined, { month: "short", day: "numeric", timeZone: "UTC" });
let session = null;

async function api(path, { data, signal, token, method = data ? "POST" : "GET" } = {}) {
  const headers = { Accept: "application/json" };
  if (data) headers["Content-Type"] = "application/json";
  if (token) headers.Authorization = `Bearer ${token}`;
  const response = await fetch(path, {
    method, headers, signal,
    body: data ? JSON.stringify(data) : undefined
  });
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    const details = Object.values(body?.errors ?? {}).flat().join(" ");
    const error = new Error(response.status >= 500
      ? "FitTrack couldn’t load this data. Try again."
      : details || body?.message || `Request failed (${response.status}).`);
    error.status = response.status;
    throw error;
  }
  return response.status === 204 ? null : response.json();
}

function text(tag, value, className) {
  const node = document.createElement(tag);
  node.textContent = value;
  if (className) node.className = className;
  return node;
}

function signOut(message = "") {
  session?.request?.abort();
  session?.progressRequest?.abort();
  session = null;
  resetTrainer();
  find("progress-chart").replaceChildren();
  find("progress-exercise").replaceChildren();
  message("progress-message", "");
  message("history-message", "");
  metricsForm.reset(); delete metricsForm.dataset.dirty;
  for (const editor of [metricsForm, workoutForm]) delete editor.dataset.saving;
  metricsForm.querySelector("button").disabled = false;
  resetWorkout();
  message("metrics-message", "");
  message("workout-message", "");
  for (const id of ["account", "weeks", "metrics", "workouts", "status"]) find(id).replaceChildren();
  find("dashboard").hidden = true;
  find("logout").hidden = true;
  find("login").hidden = false;
  find("login-error").textContent = message;
  find("login-error").hidden = !message;
  find("email").focus();
}

function renderWeeks(weeks) {
  const max = Math.max(1, ...weeks.map((week) => week.totalVolume));
  find("weeks").replaceChildren(...weeks.map((week) => {
    const row = document.createElement("div");
    row.className = "bar-row";
    const track = document.createElement("div");
    track.className = "track";
    track.setAttribute("aria-hidden", "true");
    const bar = document.createElement("div");
    bar.className = "bar";
    bar.style.width = `${week.totalVolume / max * 100}%`;
    track.append(bar);
    const value = text("span", number.format(week.totalVolume));
    value.title = `${week.workoutsCount} workouts · ${week.totalMinutes} min`;
    row.append(text("span", shortDate.format(new Date(week.weekStart))), track, value);
    return row;
  }));
}

function renderMetrics(metrics) {
  if (!metricsForm.dataset.dirty && !metricsForm.dataset.saving) {
    find("height").value = metrics.heightCm;
    find("weight").value = metrics.weightKg;
    find("goal").value = metrics.goal;
  }
  const list = document.createElement("dl");
  list.className = "metric-list";
  const fields = [
    ["Height", `${metrics.heightCm} cm`],
    ["Weight", `${metrics.weightKg} kg`],
    ["Goal", ["Lose weight", "Gain muscle", "Maintain"][metrics.goal] || "—"]
  ];
  for (const [label, value] of fields) {
    const row = document.createElement("div");
    row.append(text("dt", label), text("dd", value));
    list.append(row);
  }
  find("metrics").replaceChildren(text("p", metrics.bmi.toFixed(1), "bmi"),
    text("p", `BMI · ${metrics.bmiCategory}`, "muted"), list);
}

function renderWorkouts({ items, totalCount, page, pageSize }) {
  if (!items.length && page > 1) {
    session.historyPage = Math.max(1, Math.ceil(totalCount / pageSize));
    void loadDashboard();
    return;
  }
  find("history-prev").disabled = page <= 1;
  find("history-next").disabled = page * pageSize >= totalCount;
  find("history-page").textContent = `Page ${page}`;
  find("workouts").replaceChildren(...(items.length ? items.map(workout => {
    const row = workoutSummary(workout);
    const buttons = text("div", "", "actions");
    buttons.append(action("Edit", () => void editWorkout(workout.id)),
      action("Delete", event => void removeWorkout(workout, event.currentTarget)));
    row.append(buttons);
    return row;
  }) : [text("p", "No workouts logged yet.", "muted")]));
}

async function loadDashboard() {
  const current = session;
  if (!current) return;
  current.request?.abort();
  const request = new AbortController();
  current.request = request;
  const options = { signal: request.signal, token: current.token };
  const status = find("status");
  status.hidden = false;
  status.textContent = "Loading training data…";
  const panels = [
    ["weeks", "/api/stats/weekly?weeks=4", renderWeeks],
    ["metrics", "/api/me/metrics", renderMetrics],
    ["workouts", `/api/workouts?page=${current.historyPage}&pageSize=5`, renderWorkouts]
  ];
  const results = await Promise.allSettled(panels.map(([, path]) => api(path, options)));
  if (session !== current || request.signal.aborted) return;
  if (results.some((result) => result.reason?.status === 401 || result.reason?.status === 403)) {
    const forbidden = results.some((result) => result.reason?.status === 403);
    signOut(forbidden ? "Use your Member account to open this dashboard." : "Your session expired. Sign in again.");
    return;
  }
  results.forEach((result, index) => {
    const [id, , render] = panels[index];
    if (result.status === "fulfilled") {
      render(result.value);
    } else {
      const message = id === "metrics" && result.reason.status === 404
        ? "No body metrics saved yet. Use the form below." : "Couldn’t load this section. Try refreshing.";
      find(id).replaceChildren(text("p", message, "muted"));
    }
  });
  status.textContent = "";
  status.hidden = true;
}

form.addEventListener("submit", async (event) => {
  event.preventDefault();
  const button = form.querySelector("button");
  const credentials = Object.fromEntries(new FormData(form));
  button.disabled = true;
  find("login-error").hidden = true;
  try {
    const result = await api("/api/auth/login", { data: credentials });
    session = { token: result.accessToken, role: tokenRole(result.accessToken), historyPage: 1 };
    form.reset();
    find("account").textContent = credentials.email;
    find("login").hidden = true;
    find("logout").hidden = false;
    if (session.role === "Trainer") {
      find("trainer-dashboard").hidden = false;
      find("trainer-account").textContent = credentials.email;
      find("trainer-refresh").focus();
      await Promise.all([loadTrainerCatalog(session), loadClients(session)]);
    } else {
      find("dashboard").hidden = false;
      find("refresh").focus();
      await Promise.all([loadDashboard(), loadExerciseChoices(session)]);
    }
  } catch (error) {
    signOut(error.message);
  } finally {
    button.disabled = false;
  }
});

find("logout").addEventListener("click", () => signOut());
find("refresh").addEventListener("click", () => {
  void loadDashboard();
  if (session && !session.exercises?.length) void loadExerciseChoices(session);
});


const metricsForm = find("metrics-form");
const workoutForm = find("workout-form");
const setRows = find("set-rows");

function message(id, value, isError = false) {
  const node = find(id);
  node.textContent = value;
  node.classList.toggle("error", isError);
  node.hidden = !value;
}

function resetWorkout() {
  workoutForm.reset();
  setRows.replaceChildren();
  if (session) session.workoutId = null;
  find("workout-heading").textContent = "Log a workout";
  find("save-workout").textContent = "Save workout";
  find("cancel-workout").hidden = true;
  find("workout-date").value = localDateInput(new Date());
  if (session?.exercises?.length) addSet();
  updateSetButtons();
}

function updateSetButtons() {
  const available = Boolean(session?.exercises?.length);
  find("add-set").disabled = !available || setRows.children.length >= 100;
  find("save-workout").disabled = !available || setRows.children.length === 0;
  for (const button of setRows.querySelectorAll("button")) {
    button.disabled = setRows.children.length === 1;
  }
}

function addSet(values = null) {
  const row = document.createElement("fieldset");
  row.className = "set-row";
  row.innerHTML = '<legend>Workout set</legend><label>Exercise<select name="exerciseId" required></select></label>'
    + '<label>Reps<input name="reps" type="number" min="1" max="100" value="10" required></label>'
    + '<label>Weight (kg)<input name="weightKg" type="number" min="0" max="500" step="0.01" value="0" required></label>'
    + '<button type="button" class="secondary">Remove</button>';
  const select = row.querySelector("select");
  for (const exercise of session.exercises) {
    const option = text("option", exercise.name);
    option.value = exercise.id;
    select.append(option);
  }
  const previous = setRows.lastElementChild?.querySelector("select");
  if (previous) select.value = previous.value;
  if (values) {
    select.value = values.exerciseId;
    row.querySelector('[name="reps"]').value = values.reps;
    row.querySelector('[name="weightKg"]').value = values.weightKg;
  }
  row.querySelector("button").addEventListener("click", () => {
    row.remove();
    updateSetButtons();
  });
  setRows.append(row);
  updateSetButtons();
}

async function loadExerciseChoices(current) {
  message("workout-message", "Loading exercises…");
  const exercises = [];
  try {
    let page = 1;
    let result;
    do {
      result = await api(`/api/exercises?page=${page}&pageSize=100`, { token: current.token });
      if (session !== current) return;
      exercises.push(...result.items);
      page++;
    } while (result.items.length && exercises.length < result.totalCount);
    current.exercises = exercises;
    setOptions(find("progress-exercise"), exercises, exercise => exercise.id, exercise => exercise.name, "Choose an exercise");
    resetWorkout();
    message("workout-message", exercises.length ? "" : "Ask a Trainer to add an exercise before you log a workout.");
  } catch (error) {
    if (session !== current) return;
    message("workout-message", "Couldn’t load exercises. Use Refresh data to try again.", true);
  }
}

async function save(form, messageId, path, method, data, onSaved) {
  const current = session;
  if (!current || form.dataset.saving) return;
  form.dataset.saving = "true";
  const button = form.querySelector('[type="submit"]');
  button.disabled = true;
  message(messageId, "Saving…");
  try {
    const result = await api(path, { token: current.token, method, data });
    if (session !== current) return;
    onSaved(result);
    message(messageId, "Saved.");
    if (current.role === "Member") void loadDashboard();
  } catch (error) {
    if (session !== current) return;
    if (error.status === 401) {
      signOut("Your session expired. Sign in again.");
      return;
    }
    message(messageId, error.message, true);
  } finally {
    if (session === current) {
      delete form.dataset.saving;
      button.disabled = false;
    }
  }
}

metricsForm.addEventListener("submit", (event) => {
  event.preventDefault();
  const fields = new FormData(metricsForm);
  void save(metricsForm, "metrics-message", "/api/me/metrics", "PUT", {
    heightCm: Number(fields.get("height")),
    weightKg: Number(fields.get("weight")),
    goal: Number(fields.get("goal"))
  }, renderMetrics);
});

workoutForm.addEventListener("submit", (event) => {
  event.preventDefault();
  const fields = new FormData(workoutForm);
  const date = new Date(fields.get("date"));
  if (!Number.isFinite(date.getTime()) || date > new Date()) {
    message("workout-message", "Choose a date and time that has already passed.", true);
    return;
  }
  const setNumbers = new Map();
  const sets = Array.from(setRows.children, (row) => {
    const exerciseId = Number(row.querySelector("select").value);
    const setNumber = (setNumbers.get(exerciseId) || 0) + 1;
    setNumbers.set(exerciseId, setNumber);
    return {
      exerciseId, setNumber,
      reps: Number(row.querySelector('[name="reps"]').value),
      weightKg: Number(row.querySelector('[name="weightKg"]').value)
    };
  });
  if (sets.some((set) => set.setNumber > 20)) {
    message("workout-message", "Use at most 20 sets for each exercise.", true);
    return;
  }
  const id = session.workoutId;
  void save(workoutForm, "workout-message", id ? `/api/workouts/${id}` : "/api/workouts", id ? "PUT" : "POST", {
    date: date.toISOString(), title: fields.get("title").trim(), notes: fields.get("notes"),
    durationMinutes: Number(fields.get("duration")), sets
  }, () => {
    if (!id) session.historyPage = 1;
    resetWorkout();
    void loadProgress();
  });
});

find("add-set").addEventListener("click", () => addSet());
metricsForm.addEventListener("input", () => { metricsForm.dataset.dirty = "true"; });

function tokenRole(token) {
  const payload = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
  return JSON.parse(atob(payload)).role;
}

async function allPages(path, current) {
  const items = [];
  for (let page = 1; ; page++) {
    const result = await api(`${path}?page=${page}&pageSize=50`, { token: current.token });
    if (session !== current) return [];
    items.push(...result.items);
    if (!result.items.length || items.length >= result.totalCount) return items;
  }
}

function action(label, handler) {
  const button = text("button", label, "secondary");
  button.type = "button";
  button.addEventListener("click", handler);
  return button;
}

function setOptions(select, items, id, label, placeholder) {
  const selected = select.value;
  const empty = text("option", placeholder);
  empty.value = "";
  select.replaceChildren(empty, ...items.map(item => {
    const option = text("option", label(item));
    option.value = id(item);
    return option;
  }));
  if (items.some(item => String(id(item)) === selected)) select.value = selected;
}

async function loadProgress() {
  const current = session;
  const select = find("progress-exercise");
  const exerciseId = select.value;
  const chart = find("progress-chart");
  const request = new AbortController();
  current.progressRequest?.abort();
  current.progressRequest = request;
  chart.replaceChildren();
  if (!exerciseId) {
    message("progress-message", "");
    return;
  }
  message("progress-message", "Loading progress…");
  try {
    const points = await api(`/api/stats/exercises/${exerciseId}`, {
      token: current.token, signal: request.signal
    });
    if (session !== current || request.signal.aborted) return;
    message("progress-message", points.length ? "" : "No sets logged for this exercise yet.");
    renderBars(chart, points, point => point.date, point => point.maxWeightKg, " kg");
  } catch (error) {
    if (session === current && !request.signal.aborted) showError("progress-message", error);
  }
}

function showError(id, error) {
  if (error.status === 401) signOut("Your session expired. Sign in again.");
  else message(id, error.message, true);
}

function renderBars(container, rows, date, value, suffix) {
  const max = Math.max(1, ...rows.map(value));
  container.replaceChildren(...rows.map(row => {
    const line = text("div", "", "bar-row");
    const track = text("div", "", "track");
    const bar = text("div", "", "bar");
    bar.style.width = `${value(row) / max * 100}%`;
    track.setAttribute("aria-hidden", "true");
    track.append(bar);
    line.append(text("span", shortDate.format(new Date(date(row)))), track,
      text("span", number.format(value(row)) + suffix));
    return line;
  }));
}

function workoutSummary(workout) {
  const row = text("div", "", "workout");
  row.append(text("strong", workout.title), text("p",
    `${shortDate.format(new Date(workout.date.slice(0, 10)))} · ${workout.durationMinutes} min · Volume ${number.format(workout.totalVolume)}`, "muted"));
  return row;
}

async function editWorkout(id) {
  const current = session;
  if (workoutForm.dataset.saving) return;
  const revision = current.editRevision = (current.editRevision || 0) + 1;
  try {
    const workout = await api(`/api/workouts/${id}`, { token: current.token });
    if (session !== current || current.editRevision !== revision || workoutForm.dataset.saving) return;
    resetWorkout();
    current.workoutId = id;
    find("workout-heading").textContent = "Edit workout";
    find("save-workout").textContent = "Update workout";
    find("cancel-workout").hidden = false;
    find("workout-title").value = workout.title;
    const utcDate = /(?:Z|[+-]\d{2}:\d{2})$/.test(workout.date) ? workout.date : workout.date + "Z";
    find("workout-date").value = localDateInput(new Date(utcDate));
    find("duration").value = workout.durationMinutes;
    find("notes").value = workout.notes || "";
    setRows.replaceChildren();
    for (const exercise of workout.sets) {
      for (const set of exercise.sets) addSet({ ...set, exerciseId: exercise.exerciseId });
    }
    find("workout-title").focus();
  } catch (error) {
    if (session === current && current.editRevision === revision) showError("history-message", error);
  }
}

async function removeWorkout(workout, button) {
  if (!confirm(`Delete “${workout.title}” and all its sets?`)) return;
  const current = session;
  button.disabled = true;
  try {
    await api(`/api/workouts/${workout.id}`, { token: current.token, method: "DELETE" });
    if (session !== current) return;
    if (current.workoutId === workout.id) resetWorkout();
    message("history-message", "Workout deleted.");
    await Promise.all([loadDashboard(), loadProgress()]);
  } catch (error) {
    if (session === current) showError("history-message", error);
  } finally {
    button.disabled = false;
  }
}

function localDateInput(date) {
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 19);
}

find("cancel-workout").addEventListener("click", () => {
  session.editRevision = (session.editRevision || 0) + 1;
  resetWorkout();
  message("workout-message", "");
});
find("progress-exercise").addEventListener("change", () => void loadProgress());
for (const [id, offset] of [["history-prev", -1], ["history-next", 1]]) {
  find(id).addEventListener("click", () => {
    session.historyPage += offset;
    find("history-prev").disabled = find("history-next").disabled = true;
    void loadDashboard();
  });
}

const exerciseForm = find("exercise-form");

function resetExercise() {
  exerciseForm.reset();
  if (session) session.exerciseId = null;
  find("exercise-heading").textContent = "Add exercise";
  find("cancel-exercise").hidden = true;
}

function renderTrainerCatalog(exercises, groups) {
  const current = session;
  current.catalog = exercises;
  current.groups = groups;
  const groupList = find("group-options");
  const selected = new Set([...groupList.querySelectorAll(":checked")].map(input => input.value));
  groupList.replaceChildren(...groups.map(group => {
    const label = text("label", group.name);
    const checkbox = document.createElement("input");
    checkbox.type = "checkbox";
    checkbox.name = "group";
    checkbox.value = group.id;
    checkbox.checked = selected.has(String(group.id));
    label.prepend(checkbox);
    return label;
  }));
  const rows = exercises.map(exercise => {
    const row = text("div", "", "workout");
    const controls = text("div", "", "actions");
    controls.append(action("Edit", () => {
      if (exerciseForm.dataset.saving) return;
      resetExercise();
      current.exerciseId = exercise.id;
      find("exercise-heading").textContent = "Edit exercise";
      find("cancel-exercise").hidden = false;
      exerciseForm.elements.namedItem("name").value = exercise.name;
      exerciseForm.elements.namedItem("description").value = exercise.description || "";
      exerciseForm.elements.namedItem("equipment").value = exercise.equipment;
      for (const input of groupList.querySelectorAll("input")) {
        input.checked = exercise.muscleGroups.includes(groups.find(group => group.id === Number(input.value))?.name);
      }
      message("exercise-message", "");
      exerciseForm.elements.namedItem("name").focus();
    }), action("Delete", async event => {
      if (!confirm(`Delete “${exercise.name}”?`)) return;
      const button = event.currentTarget;
      button.disabled = true;
      try {
        await api(`/api/exercises/${exercise.id}`, { token: current.token, method: "DELETE" });
        if (session !== current) return;
        if (current.exerciseId === exercise.id) resetExercise();
        message("trainer-message", "Exercise deleted.");
        await loadTrainerCatalog(current);
      } catch (error) {
        if (session === current) showError("trainer-message", error);
      } finally {
        button.disabled = false;
      }
    }));
    row.append(text("strong", exercise.name), text("p", exercise.muscleGroups.join(", "), "muted"), controls);
    return row;
  });
  find("trainer-exercises").replaceChildren(...(rows.length ? rows : [text("p", "No exercises yet.", "muted")]));
}

async function loadTrainerCatalog(current) {
  const revision = current.catalogRevision = (current.catalogRevision || 0) + 1;
  try {
    const [exercises, groups] = await Promise.all([
      allPages("/api/exercises", current), allPages("/api/muscle-groups", current)
    ]);
    if (session === current && current.catalogRevision === revision) renderTrainerCatalog(exercises, groups);
  } catch (error) {
    if (session === current && current.catalogRevision === revision) showError("trainer-message", error);
  }
}

async function loadClients(current) {
  try {
    const clients = await allPages("/api/trainer/clients", current);
    if (session !== current) return;
    current.clients = clients;
    setOptions(find("client-picker"), clients, client => client.clientId,
      client => `${client.fullName} · ${client.email}`, clients.length ? "Choose a client" : "No clients yet");
    await loadClientWorkouts();
  } catch (error) {
    if (session === current) showError("client-workout-message", error);
  }
}

async function loadClientWorkouts() {
  const current = session;
  const id = find("client-picker").value;
  const revision = current.clientRevision = (current.clientRevision || 0) + 1;
  const container = find("client-workouts");
  container.replaceChildren();
  if (!id) {
    message("client-workout-message", "");
    return;
  }
  message("client-workout-message", "Loading workouts…");
  try {
    const workouts = await allPages(`/api/trainer/clients/${id}/workouts`, current);
    if (session !== current || current.clientRevision !== revision) return;
    const date = current.clients.find(client => client.clientId === Number(id))?.lastWorkoutDate;
    message("client-workout-message", date
      ? `Last workout · ${shortDate.format(new Date(date.slice(0, 10)))}` : "No workouts logged yet.");
    container.replaceChildren(...workouts.map(workout => {
      const row = workoutSummary(workout);
      for (const group of workout.sets) {
        row.append(text("p", `${group.exerciseName} · ${group.sets.map(set => `${set.reps} × ${set.weightKg} kg`).join(", ")}`, "muted"));
      }
      return row;
    }));
  } catch (error) {
    if (session === current && current.clientRevision === revision) showError("client-workout-message", error);
  }
}

async function trainerSave(form, id, path, method, data, refresh) {
  const current = session;
  await save(form, id, path, method, data, () => {
    if (session === current) refresh(current);
  });
}

find("group-form").addEventListener("submit", event => {
  event.preventDefault();
  const form = event.currentTarget;
  const name = new FormData(form).get("name").trim();
  void trainerSave(form, "group-message", "/api/muscle-groups", "POST", { name }, current => {
    form.reset();
    void loadTrainerCatalog(current);
  });
});

exerciseForm.addEventListener("submit", event => {
  event.preventDefault();
  const fields = new FormData(exerciseForm);
  const id = session.exerciseId;
  const muscleGroupIds = fields.getAll("group").map(Number);
  if (!muscleGroupIds.length) {
    message("exercise-message", "Select at least one muscle group.", true);
    return;
  }
  void trainerSave(exerciseForm, "exercise-message", id ? `/api/exercises/${id}` : "/api/exercises",
    id ? "PUT" : "POST", {
      name: fields.get("name").trim(), description: fields.get("description"),
      equipment: Number(fields.get("equipment")), muscleGroupIds
    }, current => {
      resetExercise();
      void loadTrainerCatalog(current);
    });
});

find("client-form").addEventListener("submit", event => {
  event.preventDefault();
  const form = event.currentTarget;
  const email = new FormData(form).get("email").trim();
  void trainerSave(form, "client-message", "/api/trainer/clients", "POST", { email }, current => {
    form.reset();
    void loadClients(current);
  });
});

find("cancel-exercise").addEventListener("click", () => {
  if (!exerciseForm.dataset.saving) resetExercise();
});
find("client-picker").addEventListener("change", () => void loadClientWorkouts());
find("trainer-refresh").addEventListener("click", () => {
  void loadTrainerCatalog(session);
  void loadClients(session);
});

function resetTrainer() {
  for (const id of ["group-form", "exercise-form", "client-form"]) {
    const form = find(id);
    form.reset();
    delete form.dataset.saving;
    form.querySelector('[type="submit"]').disabled = false;
  }
  resetExercise();
  for (const id of ["trainer-account", "group-options", "trainer-exercises", "client-picker", "client-workouts"]) {
    find(id).replaceChildren();
  }
  for (const id of ["group-message", "exercise-message", "trainer-message", "client-message", "client-workout-message"]) message(id, "");
  find("trainer-dashboard").hidden = true;
}
