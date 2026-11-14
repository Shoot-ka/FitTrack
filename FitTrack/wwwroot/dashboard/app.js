const find = (id) => document.getElementById(id);
const form = find("login-form");
const number = new Intl.NumberFormat(undefined, { maximumFractionDigits: 2 });
const shortDate = new Intl.DateTimeFormat(undefined, { month: "short", day: "numeric", timeZone: "UTC" });
let session = null;

async function api(path, { data, signal, token } = {}) {
  const headers = { Accept: "application/json" };
  if (data) headers["Content-Type"] = "application/json";
  if (token) headers.Authorization = `Bearer ${token}`;
  const response = await fetch(path, {
    method: data ? "POST" : "GET", headers, signal,
    body: data ? JSON.stringify(data) : undefined
  });
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    const error = new Error(response.status >= 500
      ? "FitTrack couldn’t load this data. Try again."
      : body?.message || `Request failed (${response.status}).`);
    error.status = response.status;
    throw error;
  }
  return response.json();
}

function text(tag, value, className) {
  const node = document.createElement(tag);
  node.textContent = value;
  if (className) node.className = className;
  return node;
}

function signOut(message = "") {
  session?.request?.abort();
  session = null;
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

function renderWorkouts({ items }) {
  find("workouts").replaceChildren(...(items.length ? items.map((workout) => {
    const row = document.createElement("div");
    row.className = "workout";
    const date = shortDate.format(new Date(workout.date.slice(0, 10)));
    row.append(text("strong", workout.title),
      text("p", `${date} · ${workout.durationMinutes} min · Volume ${number.format(workout.totalVolume)}`, "muted"));
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
    ["workouts", "/api/workouts?page=1&pageSize=5", renderWorkouts]
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
        ? "No body metrics saved yet. Add them in Swagger." : "Couldn’t load this section. Try refreshing.";
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
    session = { token: result.accessToken };
    form.reset();
    find("account").textContent = credentials.email;
    find("login").hidden = true;
    find("dashboard").hidden = false;
    find("logout").hidden = false;
    find("refresh").focus();
    await loadDashboard();
  } catch (error) {
    signOut(error.message);
  } finally {
    button.disabled = false;
  }
});

find("logout").addEventListener("click", () => signOut());
find("refresh").addEventListener("click", loadDashboard);

