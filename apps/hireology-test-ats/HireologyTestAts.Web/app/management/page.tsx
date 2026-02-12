export default function ManagementPage() {
  return (
    <div>
      <h1 className="text-2xl font-bold text-slate-900 mb-2">Management</h1>
      <p className="text-slate-600 mb-6">
        Manage groups, locations (organizations), and user access.
      </p>
      <ul className="space-y-2">
        <li>
          <a
            href="/management/groups"
            className="text-indigo-600 hover:text-indigo-800 font-medium"
          >
            Groups
          </a>
          <span className="text-slate-500 text-sm ml-2">
            — Add and edit groups (e.g. dealership groups).
          </span>
        </li>
        <li>
          <a
            href="/management/organizations"
            className="text-indigo-600 hover:text-indigo-800 font-medium"
          >
            Locations (Organizations)
          </a>
          <span className="text-slate-500 text-sm ml-2">
            — Add and edit locations under a group.
          </span>
        </li>
        <li>
          <a
            href="/management/users"
            className="text-indigo-600 hover:text-indigo-800 font-medium"
          >
            Users
          </a>
          <span className="text-slate-500 text-sm ml-2">
            — Assign users to groups or locations.
          </span>
        </li>
      </ul>
    </div>
  );
}
